using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Core's camera status message plus <c>cameraPrivacySupported</c> and <c>cameraAutoFocusOn</c>.
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

        /// <summary>
        /// Whether auto focus is on, as last reported by the camera. Null when the camera has no auto focus mode.
        /// </summary>
        [JsonProperty("cameraAutoFocusOn", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CameraAutoFocusOn { get; set; }
    }
}
