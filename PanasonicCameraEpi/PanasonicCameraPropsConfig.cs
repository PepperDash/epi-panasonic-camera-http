using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Core.WebApi.Presets;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Devices.Common.Cameras;
using Newtonsoft.Json;

namespace PanasonicCameraEpi
{
    public class PanasonicCameraPropsConfig : CameraPropertiesConfig
    {
        public static PanasonicCameraPropsConfig FromDeviceConfig(DeviceConfig config)
        {
            return JsonConvert.DeserializeObject<PanasonicCameraPropsConfig>(config.Properties.ToString());
        }
   
        [JsonProperty("control")]
        public PanasonicControlPropertiesConfig Control { get; set; }
        
        public PanasonicCameraPropsConfig()
        {
            Presets = new List<PanasonicCameraPreset>();
        }

        [JsonProperty("communicationMonitor")]
        public CommunicationMonitorConfig CommunicationMonitor { get; set; }

        [JsonProperty("presets")]
        public List<PanasonicCameraPreset> Presets { get; set; }

        public int PanSpeed { get; set; }
        public int ZoomSpeed { get; set; }
        public int TiltSpeed { get; set; }
    }

    public class PanasonicControlPropertiesConfig
    {
        public string Method { get; set; }
        public PanasonicControlPropertiesDetails TcpSshProperties { get; set; }
    }

    public class PanasonicControlPropertiesDetails
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }
}