using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
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

        [JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }

        [JsonProperty("communicationMonitor")]
        public CommunicationMonitorConfig CommunicationMonitor { get; set; }

        [JsonProperty("presets")]
        public List<PanasonicCameraPreset> Presets { get; set; }
    }
}