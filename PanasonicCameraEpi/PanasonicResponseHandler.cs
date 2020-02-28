using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicResponseHandler
    {
        private string comsRx;
        public StringFeedback ComsFb { get; private set; }

        public event EventHandler CameraPoweredOn;
        public event EventHandler CameraPoweredOff;

        public PanasonicResponseHandler()
        {
			ComsFb = new StringFeedback(() => comsRx ?? string.Empty);
        }

        public void HandleResponseRecived(object sender, GenericCommMethodReceiveTextArgs e)
        {
			Debug.Console(2, "HandleResponseRecived Response:{0}\r", e.Text);
        }

		public void HandleResponseRecived(object sender, GenericHttpClientEventArgs e)
		{
			Debug.Console(2, "Http HandleResponseRecived: {0} Response:{1}, Error: {2}\r", e.RequestPath, e.ResponseText, e.Error);
			comsRx = e.ResponseText;
			ProcessComs(comsRx);
			ComsFb.FireUpdate();
		}
        void ProcessComs(string coms)
        {
            if (coms.Equals("200 OK \"p1\"")) OnCameraPowerdOn();
            else if (coms.Equals("200 OK \"p0\"")) OnCameraPowerdOff();
            else return;
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