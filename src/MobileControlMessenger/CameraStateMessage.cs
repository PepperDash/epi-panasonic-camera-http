using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Core's camera status message plus <c>cameraPrivacySupported</c>.
    /// </summary>
    /// <remarks>
    /// Deliberately named <c>CameraStateMessage</c>: <c>DeviceMessageBase.MessageType</c> is the runtime type
    /// name, and Mobile Control clients expect "CameraStateMessage". It lives in the plugin namespace so it
    /// does not clash with the core type it extends.
    /// </remarks>
    public class CameraStateMessage : PepperDash.Essentials.AppServer.Messengers.CameraStateMessage
    {
        [JsonProperty("cameraPrivacySupported", NullValueHandling = NullValueHandling.Ignore)]
        public bool CameraPrivacySupported { get; set; }
    }
}
