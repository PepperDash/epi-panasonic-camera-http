using System;
using PepperDash.Essentials.Core;
using PepperDash.Core;
using Serilog.Events;

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
			ComsFb = new StringFeedback("ComsFeedback", () => _comsRx ?? string.Empty);
        }

		public void HandleHttpResponse(object sender, HttpResponse response)
		{
			Debug.LogMessage(LogEventLevel.Information, "Received HTTP Response: Code={0}, Content={1}", response.StatusCode, response.Content);
			_comsRx = response.Content;
			ProcessComs(_comsRx);
			ComsFb.FireUpdate();
		}

        void ProcessComs(string coms)
        {
            if (coms.Contains("p1")) 
                OnCameraPoweredOn();

            else if (coms.Contains("p0")) 
                OnCameraPoweredOff();
        }

        void OnCameraPoweredOn()
        {
            var handler = CameraPoweredOn;
            if (handler == null) return;

            handler.Invoke(this, EventArgs.Empty);
        }

        void OnCameraPoweredOff()
        {
            var handler = CameraPoweredOff;
            if (handler == null) return;

            handler.Invoke(this, EventArgs.Empty);
        }
    }
}