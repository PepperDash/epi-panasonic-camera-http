using System;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.AppServer.Messengers
{
    /// <summary>
    /// Mobile Control messenger for devices implementing <see cref="IHasCameraPrivacy"/>.
    /// </summary>
    /// <remarks>
    /// Plugin-local: Essentials core (3.0.0-rc.20) has no messenger registered for IHasCameraPrivacy.
    /// Named after the core interface so it can be dropped in favour of a core messenger if one is added.
    /// There is deliberately no privacy-state feedback, matching the interface: the camera cannot report
    /// where it is pointed, so this only advertises support and accepts commands.
    /// </remarks>
    public class IHasCameraPrivacyMessenger : MessengerBase
    {
        private readonly IHasCameraPrivacy _privacy;

        public IHasCameraPrivacyMessenger(string key, string messagePath, IHasCameraPrivacy privacy)
            : base(key, messagePath, privacy)
        {
            _privacy = privacy ?? throw new ArgumentNullException(nameof(privacy));
        }

        protected override void RegisterActions()
        {
            base.RegisterActions();

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));
            AddAction("/privacyStatus", (id, content) => SendFullStatus(id));

            AddAction("/privacyOn", (id, content) => _privacy.PrivacyOn());
            AddAction("/privacyOff", (id, content) => _privacy.PrivacyOff());
        }

        private void SendFullStatus(string id = null)
        {
            PostStatusMessage(new IHasCameraPrivacyStateMessage { HasCameraPrivacy = true }, id);
        }
    }
}
