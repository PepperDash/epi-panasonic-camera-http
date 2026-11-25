using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using PepperDash.Core;
using PepperDash.Core.Logging;

namespace PanasonicCameraEpi
{
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public string Content { get; set; }
    }

    /// <summary>
    /// Serialized, paced HTTP command queue. Commands are enqueued without blocking the caller and are
    /// sent one at a time, in order, by a single worker thread. At least <see cref="PacingMs"/> milliseconds
    /// separate the start of consecutive requests.
    /// </summary>
    public class HttpCommandQueue : IKeyed, IDisposable
    {
        public const int DefaultPacingMs = 130;

        private const int RequestTimeoutMs = 10000;

        private readonly BlockingCollection<string> _queue = new BlockingCollection<string>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Thread _worker;
        private readonly int _pacingMs;

        private volatile string _hostname;
        private volatile bool _disposed;

        public event EventHandler<HttpResponse> ResponseReceived;

        public string Key { get; private set; }

        public bool Disposed
        {
            get { return _disposed; }
        }

        /// <summary>
        /// Host (name or IP) that commands are sent to. May be changed while running; the change applies
        /// to the next request sent.
        /// </summary>
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Hostname cannot be null or empty", nameof(value));

                _hostname = value;
            }
        }

        public HttpCommandQueue(string hostname, string parentDeviceKey, int pacingMs = DefaultPacingMs)
        {
            if (string.IsNullOrEmpty(hostname))
                throw new ArgumentException("Hostname cannot be null or empty", nameof(hostname));

            if (string.IsNullOrEmpty(parentDeviceKey))
                throw new ArgumentException("Parent device key cannot be null or empty", nameof(parentDeviceKey));

            _hostname = hostname;
            _pacingMs = pacingMs > 0 ? pacingMs : DefaultPacingMs;
            Key = $"http-{parentDeviceKey}";

            _worker = new Thread(ProcessQueue)
            {
                Name = Key + "-Thread",
                IsBackground = true
            };
            _worker.Start();
        }

        /// <summary>
        /// Queues a path to be sent. Returns immediately.
        /// </summary>
        public void EnqueueCmd(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (_disposed)
                return;

            try
            {
                _queue.Add(path);
            }
            catch (InvalidOperationException)
            {
                // queue was completed by Dispose between the check and the add
            }
        }

        private void ProcessQueue()
        {
            try
            {
                foreach (var path in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    var started = Environment.TickCount;

                    try
                    {
                        SendAndRaise(path);
                    }
                    catch (Exception)
                    {
                        // never let a single failed command (or a failure while logging it) kill the worker
                    }

                    var remaining = _pacingMs - unchecked(Environment.TickCount - started);
                    if (remaining > 0 && _cts.Token.WaitHandle.WaitOne(remaining))
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // disposing
            }
            catch (Exception ex)
            {
                this.LogError("HTTP queue worker stopped unexpectedly: {0}", ex.Message);
            }
        }

        private void SendAndRaise(string path)
        {
            var host = _hostname;

            try
            {
                var response = SendHttpRequest(host, path);
                OnResponseReceived(response);
            }
            catch (WebException ex)
            {
                this.LogWarning("Request to {0} failed: {1}", host, ex.Message);
            }
            catch (Exception ex)
            {
                this.LogError("HTTP request failed for path '{0}': {1}", path, ex.Message);
            }
        }

        private static HttpResponse SendHttpRequest(string host, string path)
        {
            var request = (HttpWebRequest)WebRequest.Create($"http://{host}/{path}");
            request.Method = "GET";
            request.Timeout = RequestTimeoutMs;
            request.ReadWriteTimeout = RequestTimeoutMs;
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version11;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                    return ReadResponse(response);
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse errorResponse)
            {
                using (errorResponse)
                    return ReadResponse(errorResponse);
            }
        }

        private static HttpResponse ReadResponse(HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return new HttpResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Content = reader.ReadToEnd()
                };
            }
        }

        private void OnResponseReceived(HttpResponse response)
        {
            if (response.StatusCode < 200 || response.StatusCode >= 300)
            {
                this.LogWarning("Panasonic camera http code error: {0}", response.StatusCode);
                return;
            }

            try
            {
                ResponseReceived?.Invoke(this, response);
            }
            catch (Exception ex)
            {
                this.LogError("Panasonic camera response handler exception: {0}", ex.Message);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _queue.CompleteAdding();
            _cts.Cancel();
        }
    }
}
