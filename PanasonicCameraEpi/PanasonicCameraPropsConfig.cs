using System.Collections.Generic;
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
        public string HomeCommand { get; set; }
        public string PrivacyCommand { get; set; }
        public int pacing { get; set; }

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