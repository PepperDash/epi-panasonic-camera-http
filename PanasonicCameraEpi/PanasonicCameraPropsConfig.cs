using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core.Config;
using Newtonsoft.Json;

namespace PanasonicCameraEpi
{
    public class PanasonicCameraPropsConfig
    {
        public static PanasonicCameraPropsConfig FromDeviceConfig(DeviceConfig config)
        {
            return JsonConvert.DeserializeObject<PanasonicCameraPropsConfig>(config.Properties.ToString());
        }

        public string IpAddress { get; set; }
        public List<PanasonicCameraPreset> Presets { get; set; }
    }
}