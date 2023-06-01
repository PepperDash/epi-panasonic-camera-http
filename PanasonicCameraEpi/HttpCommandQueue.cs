using System;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class HttpCommandQueue : CommandQueue
    {
        public event EventHandler<GenericHttpClientEventArgs> ResponseReceived;
        int _Pacing = 130; 

        public HttpCommandQueue(IBasicCommunication coms)
            : base(coms)
        {

        }
        public HttpCommandQueue(IBasicCommunication coms, int pacing)
            : base(coms)
        {
            _Pacing = pacing;
        }

        protected override object ProcessQueue(object obj)
        {
            var client = obj as GenericHttpClient;
            if (client == null)
                throw new NullReferenceException("client");

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
                    if(string.IsNullOrEmpty(client.Client.HostName))
                    {
                        Debug.Console(0, client, "Panasonic camera hostname not valid");
                        return null;
                    }
                    try
                    {
                        var request = new HttpClientRequest();
                        var url = String.Format("http://{0}/{1}", client.Client.HostName, path);
                        request.Url.Parse(url);

                        Debug.Console(1, client, "Dispatching request: {0}", request.Url.PathAndParams);

                        client.Client.DispatchAsync(request, OnResponseReceived);
                        Thread.Sleep(_Pacing); //command gap of 130 recommended by documentation
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(1, client, "Caught an exception in the CmdProcessor {0}\r{1}\r{2}", ex.Message, ex.InnerException, ex.StackTrace);
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

                if (ResponseReceived == null)
                    return;

                ResponseReceived.Invoke(this, new GenericHttpClientEventArgs(response.ContentString, response.ResponseUrl, HTTP_CALLBACK_ERROR.COMPLETED));

            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Panasonic camera client callback exception: {0}", ex.Message);
            }
        }
    }
}