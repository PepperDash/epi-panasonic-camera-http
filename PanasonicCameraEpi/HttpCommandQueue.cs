using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class HttpCommandQueue : CommandQueue
    {
        public event EventHandler<GenericHttpClientEventArgs> ResponseReceived;

        public HttpCommandQueue(IBasicCommunication coms)
            : base(coms)
        {

        }

        protected override object ProcessCmd(object obj)
        {
            var client = obj as GenericHttpClient;

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
                    try
                    {
                        var request = new HttpClientRequest();
                        var url = String.Format("http://{0}/{1}", client.Client.HostName, path);
                        request.Url.Parse(url);

                        Debug.Console(1, client, "Dispatching request: {0}", request.Url.PathAndParams);

                        var response = client.Client.Dispatch(request);
                        using (response)
                        {
                            if (!String.IsNullOrEmpty(response.ContentString))
                                OnResponseReceived(response);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.ConsoleWithLog(0, client, "Caught an exception in the CmdProcessor {0}\r{1}\r{2}", ex.Message, ex.InnerException, ex.StackTrace);
                    }
                }
                else _wh.Wait();
            }

            return null;
        }

        private void OnResponseReceived(HttpClientResponse response)
        {
            var handler = ResponseReceived;
            if (handler == null)
                return;

            handler.Invoke(this, new GenericHttpClientEventArgs(response.ContentString, response.ResponseUrl, HTTP_CALLBACK_ERROR.COMPLETED));
        }
    }
}