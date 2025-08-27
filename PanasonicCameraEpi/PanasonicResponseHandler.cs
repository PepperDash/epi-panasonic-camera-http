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

        public sealed class BusyChangedEventArgs : EventArgs
        {
            public bool IsBusy { get; private set; }
            public BusyChangedEventArgs(bool isBusy) { IsBusy = isBusy; }
        }

        public event EventHandler<BusyChangedEventArgs> BusyChanged;

        private void RaiseBusy(bool busy)
        {
            var handler = BusyChanged;
            if (handler != null)
                handler(this, new BusyChangedEventArgs(busy));
        }

        public PanasonicResponseHandler()
        {
            ComsFb = new StringFeedback(() => _comsRx ?? string.Empty);
        }

        public void HandleResponseReceived(object sender, GenericCommMethodReceiveTextArgs e)
        {
            Debug.Console(2, "HandleResponseReceived (Serial) Body:{0}", e.Text);
        }

        public void HandleResponseReceived(object sender, GenericHttpClientEventArgs e)
        {
            var body = (e.ResponseText ?? string.Empty).Trim();

            // Busy detection (Panasonic returns ER2 when busy)
            if (body.StartsWith("ER2", StringComparison.OrdinalIgnoreCase))
            {
                RaiseBusy(true);
                Debug.Console(1, "Device Busy: Path:{0} Body:{1} Error:{2}", e.RequestPath, body, e.Error);
                _comsRx = body;
                ComsFb.FireUpdate();
                return;
            }

            // Clear busy only when the HTTP callback succeeded
            if (e.Error == HTTP_CALLBACK_ERROR.COMPLETED)
                RaiseBusy(false);

            Debug.Console(1, "HTTP OK: Path:{0} Body:{1} Error:{2}", e.RequestPath, body, e.Error);
            _comsRx = body;
            ProcessComs(_comsRx);
            ComsFb.FireUpdate();
        }

        void ProcessComs(string coms)
        {
            if (coms.IndexOf("p1", StringComparison.OrdinalIgnoreCase) >= 0)
                OnCameraPowerdOn();
            else if (coms.IndexOf("p0", StringComparison.OrdinalIgnoreCase) >= 0)
                OnCameraPowerdOff();
        }

        void OnCameraPowerdOn()
        {
            var handler = CameraPoweredOn;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        void OnCameraPowerdOff()
        {
            var handler = CameraPoweredOff;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
