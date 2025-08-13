using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Serilog.Events;

namespace PanasonicCameraEpi
{
    public class HttpCommandQueue : IDisposable, IKeyed
    {
        public event EventHandler<HttpClientResponse> ResponseReceived;
        private int _pacing = 130;
        private readonly HttpClient _httpClient;
        private readonly string _hostname;
        private readonly CrestronQueue<string> _cmdQueue;
        private readonly Thread _worker;
        private readonly CEvent _wh = new CEvent();
        
        public string Key { get; private set; }
        public bool Disposed { get; private set; }

        public HttpCommandQueue(string hostname)
        {
            _hostname = hostname;
            _httpClient = new HttpClient();
            Key = $"http-{hostname}";
            _cmdQueue = new CrestronQueue<string>();
            _worker = new Thread(ProcessQueue, null, Thread.eThreadStartOptions.Running) {Name = Key + "-Thread"};
            
            CrestronEnvironment.ProgramStatusEventHandler += programEvent =>
            {
                if (programEvent != eProgramStatusEventType.Stopping)
                    return;

                _cmdQueue.Clear();
                Dispose();
            };
        }

        public HttpCommandQueue(string hostname, int pacing)
        {
            _hostname = hostname;
            _pacing = pacing;
            _httpClient = new HttpClient();
            Key = $"http-{hostname}";
            _cmdQueue = new CrestronQueue<string>();
            _worker = new Thread(ProcessQueue, null, Thread.eThreadStartOptions.Running) {Name = Key + "-Thread"};
            
            CrestronEnvironment.ProgramStatusEventHandler += programEvent =>
            {
                if (programEvent != eProgramStatusEventType.Stopping)
                    return;

                _cmdQueue.Clear();
                Dispose();
            };
        }
        

        public void EnqueueCmd(string cmd)
        {
            if (Disposed)
                return;

            _cmdQueue.Enqueue(cmd);
            _wh.Set();
        }
        
        private object ProcessQueue(object obj)
        {
            while (true)
            {
                string path = null;

                if (_cmdQueue.Count > 0)
                {
                    path = _cmdQueue.Dequeue();
                    if (path == null)
                        break;
                }
                if (path != null)
                {
                    if (string.IsNullOrEmpty(_hostname))
                    {
                        Debug.LogMessage(LogEventLevel.Error, this, "Panasonic camera hostname not valid");
                        return null;
                    }
                    try
                    {
                        var request = new HttpClientRequest
                        {
                            Url = new UrlParser($"http://{_hostname}/{path}"),
                            RequestType = RequestType.Get
                        };

                        Debug.LogMessage(LogEventLevel.Information, this, "Dispatching request: {0}", request.Url.PathAndParams);

                        _httpClient.DispatchAsync(request, OnResponseReceived);
                        Thread.Sleep(_pacing); //command gap of 130 recommended by documentation
                    }
                    catch (Exception ex)
                    {
                        Debug.LogMessage(LogEventLevel.Error, this, "Caught an exception in the CmdProcessor {0}\r{1}\r{2}", ex.Message, ex.InnerException, ex.StackTrace);
                    }
                }
                else _wh.Wait();
            }

            return null;
        }

        private void OnResponseReceived(HttpClientResponse response, HTTP_CALLBACK_ERROR error)
        {
            try
            {
                Debug.LogMessage(LogEventLevel.Information, this, "Panasonic camera client response code: {0}", response.Code);
                if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                {
                    Debug.LogMessage(LogEventLevel.Warning, this, "Panasonic camera client callback error: {0}", error);
                    return;
                }
                if (response.Code < 200 || response.Code >= 300)
                {
                    Debug.LogMessage(LogEventLevel.Warning, this, "Panasonic camera client callback http code error: {0}", response.Code);
                    return;
                }

                ResponseReceived?.Invoke(this, response);

            }
            catch (Exception ex)
            {
                Debug.LogMessage(LogEventLevel.Error, this, "Panasonic camera client callback exception: {0}", ex.Message);
            }
        }
        
        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            CrestronEnvironment.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                EnqueueCmd(null);
                _worker.Abort();
                _wh.Close();
                _httpClient?.Dispose();
            }

            Disposed = true;
        }

        ~HttpCommandQueue()
        {
            Dispose(false);
        }

        #endregion
    }

}