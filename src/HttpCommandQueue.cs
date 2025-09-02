using System;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class HttpCommandQueue : CommandQueue
    {
        private volatile bool _cameraBusy;

        public void WireBusy(PanasonicResponseHandler rh)
        {
            if (rh == null) return;
            rh.BusyChanged += delegate(object s, PanasonicResponseHandler.BusyChangedEventArgs e)
            {
                _cameraBusy = e.IsBusy;
            };
        }

        public event EventHandler<GenericHttpClientEventArgs> ResponseReceived;

        // Panasonic spec tolerates >=40ms; 130ms is conservative and stable
        private int _pacing = 130;

        public HttpCommandQueue(IBasicCommunication coms)
            : base(coms)
        {
        }

        public HttpCommandQueue(IBasicCommunication coms, int pacing)
            : base(coms)
        {
            _pacing = pacing;
        }

        protected override object ProcessQueue(object obj)
        {
            var client = obj as GenericHttpClient;
            if (client == null)
                throw new NullReferenceException("client");

            while (true) // keep the worker alive
            {
                string path = null;

                if (_cmdQueue.Count > 0)
                {
                    path = _cmdQueue.Dequeue();
                    if (path == null)
                    {
                        Thread.Sleep(20);
                        continue;
                    }
                }

                if (path != null)
                {
                    if (string.IsNullOrEmpty(client.Client.HostName))
                    {
                        Debug.Console(0, "Panasonic camera hostname not valid");
                        Thread.Sleep(1000); // don't kill the thread
                        continue;
                    }

                    try
                    {
                        // Back off while camera is busy (e.g., RP150 is moving it)
                        if (_cameraBusy)
                        {
                            var waited = 0;
                            while (_cameraBusy && waited < 2000) // up to 2s
                            {
                                Thread.Sleep(50);
                                waited += 50;
                            }

                            // Still busy? Requeue and try later
                            if (_cameraBusy)
                            {
                                _cmdQueue.Enqueue(path);
                                // optional: signal wait handle here if your base queue expects it
                                continue;
                            }
                        }

                        var request = new HttpClientRequest();
                        var url = string.Format("http://{0}/{1}", client.Client.HostName, path);
                        request.Url.Parse(url);

                        Debug.Console(1, "Dispatching request: {0}", request.Url.PathAndParams);

                        client.Client.DispatchAsync(request, OnResponseReceived);

                        // Panasonic pacing
                        Thread.Sleep(_pacing);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(1, "Caught an exception in the CmdProcessor {0}\r{1}\r{2}",
                            ex.Message, ex.InnerException, ex.StackTrace);
                        Thread.Sleep(100); // keep thread alive
                    }
                }
                else
                {
                    // Wait until someone enqueues
                    _wh.Wait();
                }
            }
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

                var handler = ResponseReceived;
                if (handler == null)
                    return;

                handler(this, new GenericHttpClientEventArgs(response.ContentString, response.ResponseUrl, HTTP_CALLBACK_ERROR.COMPLETED));
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Panasonic camera client callback exception: {0}", ex.Message);
            }
        }
    }
}
