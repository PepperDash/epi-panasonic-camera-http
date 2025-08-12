using System;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class HttpCommandQueue : CommandQueue
    {
        public event EventHandler<HttpClientResponse> ResponseReceived;
        private int _pacing = 130;
        private readonly HttpClient _httpClient;
        private readonly string _hostname;

        public HttpCommandQueue(string hostname)
            : base(CreateDummyCommunication(hostname))
        {
            _hostname = hostname;
            _httpClient = new HttpClient();
        }

        public HttpCommandQueue(string hostname, int pacing)
            : base(CreateDummyCommunication(hostname))
        {
            _hostname = hostname;
            _pacing = pacing;
            _httpClient = new HttpClient();
        }

        private static IBasicCommunication CreateDummyCommunication(string hostname)
        {
            // Create a minimal dummy communication object for base class compatibility
            return new DummyHttpCommunication(hostname);
        }

        protected override object ProcessQueue(object obj)
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
                    if(string.IsNullOrEmpty(_hostname))
                    {
                        Debug.Console(0, this, "Panasonic camera hostname not valid");
                        return null;
                    }
                    try
                    {
                        var request = new HttpClientRequest
                        {
                            Url = new UrlParser($"http://{_hostname}/{path}"),
                            RequestType = RequestType.Get
                        };

                        Debug.Console(1, this, "Dispatching request: {0}", request.Url.PathAndParams);

                        _httpClient.DispatchAsync(request, OnResponseReceived);
                        Thread.Sleep(_pacing); //command gap of 130 recommended by documentation
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(1, this, "Caught an exception in the CmdProcessor {0}\r{1}\r{2}", ex.Message, ex.InnerException, ex.StackTrace);
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
                Debug.Console(1, this, "Panasonic camera client response code: {0}", response.Code);
                if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                {
                    Debug.Console(1, this, "Panasonic camera client callback error: {0}", error);
                    return;
                }
                if (response.Code < 200 || response.Code >= 300)
                {
                    Debug.Console(1, this, "Panasonic camera client callback http code error: {0}", response.Code);
                    return;
                }

                ResponseReceived?.Invoke(this, response);

            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Panasonic camera client callback exception: {0}", ex.Message);
            }
        }
    }

    // Minimal dummy implementation for base class compatibility
    internal class DummyHttpCommunication : IBasicCommunication
    {
        public string Key { get; private set; }
        public bool IsConnected => true;
        public CommunicationGather LineGather { get; set; }

        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;
        public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;

        public DummyHttpCommunication(string hostname)
        {
            Key = $"http-{hostname}";
        }

        public void Connect() { }
        public void Disconnect() { }
        public void SendText(string text) { }
        public void SendBytes(byte[] bytes) { }
        public void Dispose() { }
    }
}