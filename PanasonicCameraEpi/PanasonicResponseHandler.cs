﻿using System;
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

		public void HandleResponseReceived(object sender, GenericHttpClientEventArgs e)
		{
			Debug.Console(2, "Http HandleResponseRecived: {0} Response:{1}, Error: {2}\r", e.RequestPath, e.ResponseText, e.Error);
			_comsRx = e.ResponseText;
			ProcessComs(_comsRx);
			ComsFb.FireUpdate();
		}
        void ProcessComs(string coms)
        {
            if (coms.Contains("p1")) 
                OnCameraPowerdOn();

            else if (coms.Contains("p0")) 
                OnCameraPowerdOff();

			else if (coms.Contains("s01")) 
                Debug.Console(1, "ProcessComms({0}) Preset 1 Recall Feedback", coms);
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