using Newtonsoft.Json;

namespace PepperDash.Essentials.AppServer.Messengers
{
    /// <summary>
    /// State message for <see cref="IHasCameraPrivacyMessenger"/>.
    /// </summary>
    public class IHasCameraPrivacyStateMessage : DeviceStateMessageBase
    {
        /// <summary>
        /// True when the device can be sent to, and returned from, its privacy position.
        /// </summary>
        [JsonProperty("hasCameraPrivacy", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasCameraPrivacy { get; set; }
    }
}
