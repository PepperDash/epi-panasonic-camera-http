using System;
using System.Text.RegularExpressions;
using PepperDash.Essentials.Core;
using PepperDash.Core;
using Serilog;

namespace PepperDash.Essentials.Plugins
{
    public class PanasonicResponseHandler
    {
        private string _comsRx;
        public StringFeedback ComsFb { get; private set; }

        public event EventHandler CameraPoweredOn;
        public event EventHandler CameraPoweredOff;
        public event EventHandler<ResponseDeviceInfoEventArgs> ResponseDeviceInfo;

        /// <summary>
        /// Raised when the camera reports auto focus state ("d10" / "d11", the reply to #D1 and #D1[0|1]).
        /// </summary>
        public event EventHandler<bool> AutoFocusChanged;

        public PanasonicResponseHandler()
        {
			ComsFb = new StringFeedback("ComsFeedback", () => _comsRx ?? string.Empty);
        }

		public void HandleHttpResponse(object sender, HttpResponse response)
		{
			Log.Debug("PanasonicResponseHandler: received HTTP response Code={code}, Content={content}", response.StatusCode, response.Content);
			_comsRx = response.Content;
			ProcessComs(_comsRx);
			ComsFb.FireUpdate();
		}

        void ProcessComs(string coms)
        {
            var autoFocus = Regex.Match(coms.Trim(), @"^d1(?<v>[01])$");
            if (autoFocus.Success)
            {
                AutoFocusChanged?.Invoke(this, autoFocus.Groups["v"].Value == "1");
                return;
            }

            // getinfo reply ("MAC=..", "SERIAL=..", "VERSION=.."); checked first so it is never mistaken for a power reply
            if (coms.IndexOf("mac=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ProcessDeviceInfo(coms);
                return;
            }

            if (coms.Contains("p1")) 
                OnCameraPoweredOn();

            else if (coms.Contains("p0")) 
                OnCameraPoweredOff();
        }

        void ProcessDeviceInfo(string data)
        {
            ResponseDeviceInfo?.Invoke(this, new ResponseDeviceInfoEventArgs
            {
                MacAddress = ExtractValue(data, "MAC"),
                SerialNumber = ExtractValue(data, "SERIAL"),
                FirmwareVersion = ExtractValue(data, "VERSION")
            });
        }

        static string ExtractValue(string data, string key)
        {
            return Regex.Match(data, @"(?im)^\s*" + key + @"=(?<v>.*)$").Groups["v"].Value.Trim();
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