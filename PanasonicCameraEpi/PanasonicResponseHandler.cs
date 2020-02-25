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
            if (e.Error != Crestron.SimplSharp.Net.Http.HTTP_CALLBACK_ERROR.COMPLETED) return;

            comsRx = e.ResponseText;
            ComsFb.FireUpdate();
        }

        void ProcessComs(string coms)
        {

        }
    }
}