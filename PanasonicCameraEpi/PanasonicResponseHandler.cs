using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicResponseHandler
    {
        private string comsRx;
        public StringFeedback ComsFb { get; private set; }

        public event EventHandler CameraPoweredOn;
        public event EventHandler CameraPoweredOff;

        public PanasonicResponseHandler(GenericHttpClient client)
        {
            client.ResponseRecived += HandleResponseRecived;
            ComsFb = new StringFeedback(() => comsRx ?? string.Empty);
        }

        void HandleResponseRecived(object sender, GenericHttpClientEventArgs e)
        {          
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