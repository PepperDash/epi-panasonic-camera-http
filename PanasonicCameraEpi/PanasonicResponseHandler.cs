using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Core;
using System.Text.RegularExpressions;

namespace PanasonicCameraEpi
{
    public class PanasonicResponseHandler
    {
        private string _comsRx;
        public StringFeedback ComsFb { get; private set; }

        public event EventHandler CameraPoweredOn;
        public event EventHandler CameraPoweredOff;
        public event EventHandler<ResponseDeviceInfoEventArgs> ResponseDeviceInfo;

        private const string MacPattern = @"(?<=MAC\=)\s*.*";
        private const string SerialPattern = @"(?<=SERIAL\=)\s*.*";
        private const string VersionPattern = @"(?<=VERSION\=)\s*.*";
        private const string ModelPattern = @"(?<=NAME\=)\s*.*";

        private readonly Dictionary<string, Regex> _deviceInfoRegex;

        public PanasonicResponseHandler()
        {
			ComsFb = new StringFeedback(() => _comsRx ?? string.Empty);
            _deviceInfoRegex = new Dictionary<string, Regex>()
            {
                {"mac", new Regex(MacPattern)},
			    {"serial", new Regex(SerialPattern)},
			    {"version", new Regex(VersionPattern)},
			    {"name", new Regex(ModelPattern)}
            };
        }

        public void HandleResponseReceeved(object sender, GenericCommMethodReceiveTextArgs e)
        {
			Debug.Console(2, "HandleResponseRecived Response:{0}\r", e.Text);
        }

		public void HandleResponseReceived(object sender, GenericHttpClientEventArgs e)
		{
			Debug.Console(1, "Received Response: {0} Response:{1}, Error: {2}\r", e.RequestPath, e.ResponseText, e.Error);
			_comsRx = e.ResponseText;
			ProcessComs(_comsRx);
			ComsFb.FireUpdate();
		}

        void ProcessComs(string coms)
        {
            if (coms.ToLower().Contains("mac="))
            {
                ProcessDeviceInfoData(coms);
                return;
            }
            if (coms.Contains("p1")) 
                OnCameraPowerdOn();

            else if (coms.Contains("p0")) 
                OnCameraPowerdOff();
        }

        void ProcessDeviceInfoData(string data)
        {
            var devInfo = new ResponseDeviceInfoEventArgs
            {
                MacAddress = _deviceInfoRegex["mac"].Match(data).Value,
                Serial = _deviceInfoRegex["serial"].Match(data).Value,
                Firmware = _deviceInfoRegex["version"].Match(data).Value,
                Model = _deviceInfoRegex["name"].Match(data).Value
            };

            OnResponseDeviceInfoChange(devInfo);
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

        private void OnResponseDeviceInfoChange(ResponseDeviceInfoEventArgs args)
        {
            var handler = ResponseDeviceInfo;
            if (handler == null) return;
            handler.Invoke(this, args);
        }
    }

    public class ResponseDeviceInfoEventArgs : EventArgs
    {
        public string MacAddress { get; set; }
        public string Serial { get; set; }
        public string Firmware { get; set; }
        public string Model { get; set; }
    }
}