using System;

namespace PepperDash.Essentials.Plugins
{
    public class ResponseDeviceInfoEventArgs : EventArgs
    {
        public string MacAddress { get; set; }
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }
    }
}
