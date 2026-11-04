using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Core's <see cref="CameraControlMessenger{T}"/> (pan/tilt/zoom, presets, power) extended with camera privacy.
    /// </summary>
    /// <remarks>
    /// Essentials 3.0.0-rc.20 has no Mobile Control support for <see cref="IHasCameraPrivacy"/>. This subclass adds
    /// <c>/privacyOn</c>, <c>/privacyOff</c> and a <c>cameraPrivacySupported</c> status field. See the README
    /// ("Mobile Control") for why it replaces the default camera messenger instead of sitting beside it.
    /// </remarks>
    public class PanasonicCameraControlMessenger : CameraControlMessenger<IHasCameraControls>
    {
        private readonly PanasonicCamera _camera;

        public PanasonicCameraControlMessenger(string key, PanasonicCamera camera, string messagePath)
            : base(key, camera, messagePath)
        {
            _camera = camera;
        }

        protected override void RegisterActions()
        {
            // AddAction keeps the first handler registered for a path, so register the status paths before
            // calling base to replace core's status message with the extended one.
            AddAction("/fullStatus", (id, content) => SendCameraStatus(id));
            AddAction("/cameraStatus", (id, content) => SendCameraStatus(id));

            base.RegisterActions();

            AddAction("/privacyOn", (id, content) => _camera.PrivacyOn());
            AddAction("/privacyOff", (id, content) => _camera.PrivacyOff());
        }

        // Mirrors CameraControlMessenger.SendCameraFullMessageObject (private in core) and adds cameraPrivacySupported. Remove when core handles IHasCameraPrivacy.
        private void SendCameraStatus(string id = null)
        {
            var message = new CameraStateMessage
            {
                CameraManualSupported = _camera is IHasCameraControls,
                CameraAutoSupported = _camera is IHasCameraAutoMode,
                CameraOffSupported = _camera is IHasCameraOff,
                CameraMode = GetCameraMode(),
                HasPresets = _camera is IHasCameraPresets,
                Presets = _camera.Presets,
                Capabilities = new PepperDash.Essentials.AppServer.Messengers.CameraCapabilities
                {
                    CanPan = _camera.CanPan,
                    CanTilt = _camera.CanTilt,
                    CanZoom = _camera.CanZoom,
                    CanFocus = _camera.CanFocus
                },
                IsFarEnd = _camera is IAmFarEndCamera,
                CameraPrivacySupported = _camera is IHasCameraPrivacy
            };

            PostStatusMessage(message, id);
        }

        private eCameraControlMode GetCameraMode()
        {
            if (_camera is IHasCameraAutoMode auto && auto.CameraAutoModeIsOnFeedback.BoolValue)
                return eCameraControlMode.Auto;

            if (!_camera.PowerIsOnFeedback.BoolValue)
                return eCameraControlMode.Off;

            return eCameraControlMode.Manual;
        }
    }
}
