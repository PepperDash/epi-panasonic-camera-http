using System;
using Crestron.SimplSharp.Net.Http;
using PepperDash.Essentials.Core;
using PepperDash.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicResponseHandler
    {
        private string _comsRx;
        public StringFeedback ComsFb { get; private set; }

        public event EventHandler CameraPoweredOn;
        public event EventHandler CameraPoweredOff;

        public PanasonicResponseHandler()
        {
			ComsFb = new StringFeedback(() => _comsRx ?? string.Empty);
        }

        public void HandleResponseReceeved(object sender, GenericCommMethodReceiveTextArgs e)
        {
			Debug.Console(2, "HandleResponseRecived Response:{0}\r", e.Text);
        }

		public void HandleHttpResponse(object sender, HttpClientResponse response)
		{
			Debug.Console(1, "Received HTTP Response: Code={0}, Content={1}", response.Code, response.ContentString);
			_comsRx = response.ContentString;
			ProcessComs(_comsRx);
			ComsFb.FireUpdate();
		}

        void ProcessComs(string coms)
        {
            if (coms.Contains("p1")) 
                OnCameraPowerdOn();

            else if (coms.Contains("p0")) 
                OnCameraPowerdOff();
        }

        void OnCameraPowerdOn()
        {
            var handler = CameraPoweredOn;
            if (handler == null) return;

            handler.Invoke(this, EventArgs.Empty);
        }

        void OnCameraPowerdOff()
        {
            var handler = CameraPoweredOff;
            if (handler == null) return;

            handler.Invoke(this, EventArgs.Empty);
        }
    }
}