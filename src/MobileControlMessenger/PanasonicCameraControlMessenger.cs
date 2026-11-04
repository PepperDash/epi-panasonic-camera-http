using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Core's <see cref="CameraControlMessenger{T}"/> (pan/tilt/zoom, presets, power) extended with camera privacy and focus.
    /// </summary>
    /// <remarks>
    /// Essentials 3.0.0-rc.20 has no Mobile Control support for <see cref="IHasCameraPrivacy"/>,
    /// <see cref="IHasCameraFocusControl"/> or <see cref="IHasAutoFocusMode"/>. This subclass adds <c>/privacyOn</c>,
    /// <c>/privacyOff</c>, the focus actions and the matching status fields. See the README
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

            // Press and hold, same payload and naming style as core's /cameraUp, /cameraZoomIn, ...
            AddAction("/cameraFocusNear", (id, content) =>
                PressAndHoldHandler.HandlePressAndHold(DeviceKey, "/cameraFocusNear", content, held =>
                {
                    if (held) _camera.FocusNear();
                    else _camera.FocusStop();
                }));
            AddAction("/cameraFocusFar", (id, content) =>
                PressAndHoldHandler.HandlePressAndHold(DeviceKey, "/cameraFocusFar", content, held =>
                {
                    if (held) _camera.FocusFar();
                    else _camera.FocusStop();
                }));

            AddAction("/cameraAutoFocus", (id, content) => _camera.TriggerAutoFocus());
            AddAction("/cameraFocusModeAuto", (id, content) => _camera.SetFocusModeAuto());
            AddAction("/cameraFocusModeManual", (id, content) => _camera.SetFocusModeManual());
            AddAction("/cameraFocusModeToggle", (id, content) => _camera.ToggleFocusMode());

            _camera.AutoFocusIsOnFeedback.OutputChange += (sender, args) =>
                PostStatusMessage(JToken.FromObject(new { cameraAutoFocusOn = args.BoolValue }));
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
                CameraPrivacySupported = _camera is IHasCameraPrivacy,
                CameraAutoFocusOn = _camera is IHasAutoFocusMode ? (bool?)_camera.AutoFocusIsOnFeedback.BoolValue : null
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
