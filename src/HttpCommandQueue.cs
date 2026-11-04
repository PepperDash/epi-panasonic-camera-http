using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
    /// Sends HTTP GET commands to the camera one at a time, in the order they were enqueued,
    /// using System.Net.Http. EnqueueCmd returns immediately; responses are raised on ResponseReceived.
    /// </summary>
    public class HttpCommandQueue : IKeyed, IDisposable
    {
        public event EventHandler<HttpResponse> ResponseReceived;

        private readonly string hostname;
        private readonly HttpClient _client;
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleReader = true });
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _worker;

        public string Key { get; private set; }
        public bool Disposed { get; private set; }

        public HttpCommandQueue(string hostname)
            : this(hostname, null)
        {
        }

        public HttpCommandQueue(string hostname, string parentDeviceKey)
        {
            if (string.IsNullOrEmpty(hostname))
                throw new ArgumentException("Hostname cannot be null or empty", nameof(hostname));

            this.hostname = hostname;
            Key = string.IsNullOrEmpty(parentDeviceKey) ? $"http-{hostname}" : $"http-{parentDeviceKey}";

            _client = new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
                MaxConnectionsPerServer = 10
            })
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            _worker = Task.Run(() => ProcessQueueAsync(_cts.Token));
        }

        public void EnqueueCmd(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (Disposed)
                return;

            _channel.Writer.TryWrite(path);
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            try
            {
                await foreach (var path in _channel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    await SendAsync(path, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // disposing
            }
        }

        private async Task SendAsync(string path, CancellationToken token)
        {
            var url = $"http://{hostname}/{path}";
            this.LogDebug("Sending GET request to: {0}", path);

            try
            {
                using (var message = await _client.GetAsync(url, token).ConfigureAwait(false))
                {
                    var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.LogDebug("Received HTTP response: {0}", content);
                    OnResponseReceived(new HttpResponse { StatusCode = (int)message.StatusCode, Content = content });
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                this.LogWarning("Device at {0} timed out for path '{1}'", hostname, path);
            }
            catch (HttpRequestException ex)
            {
                this.LogWarning("Device at {0} is not reachable or request failed: {1}", hostname, ex.Message);
            }
            catch (Exception ex)
            {
                this.LogError("HTTP request failed for path '{0}': {1}", path, ex.Message);
                this.LogDebug("HTTP request exception details: {0}", ex.StackTrace);
            }
        }

        private void OnResponseReceived(HttpResponse response)
        {
            try
            {
                if (response.StatusCode < 200 || response.StatusCode >= 300)
                {
                    this.LogWarning("Panasonic camera callback http code error: {0}", response.StatusCode);
                    return;
                }

                ResponseReceived?.Invoke(this, response);
            }
            catch (Exception ex)
            {
                this.LogError("Panasonic camera callback exception: {0}", ex.Message);
                this.LogDebug("Exception details: {0}", ex.StackTrace);
            }
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            _channel.Writer.TryComplete();
            _cts.Cancel();
            _client.Dispose();
            _cts.Dispose();
        }
    }
}
