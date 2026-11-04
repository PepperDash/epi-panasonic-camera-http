using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.Plugins.Tests;

/// <summary>
/// Mobile Control contract: which interfaces the camera implements (core creates messengers from them),
/// which are excluded from the default set, the replacement messenger's actions, and the status message.
/// </summary>
public class MobileControlTests
{
    // PanasonicCamera cannot be constructed off a processor (its monitor touches CrestronEnvironment),
    // so create it without running the constructor and give it the feedbacks the messenger subscribes to.
    private static PanasonicCamera UninitializedCamera()
    {
        var camera = (PanasonicCamera)RuntimeHelpers.GetUninitializedObject(typeof(PanasonicCamera));
        SetFeedback(camera, nameof(PanasonicCamera.AutoFocusIsOnFeedback));
        SetFeedback(camera, nameof(PanasonicCamera.PowerIsOnFeedback));
        return camera;
    }

    private static void SetFeedback(PanasonicCamera camera, string property) =>
        typeof(PanasonicCamera).GetProperty(property)!.SetValue(camera, new BoolFeedback(property, () => false));

    private static IReadOnlyList<string> ActionPaths(MessengerBase messenger)
    {
        typeof(MessengerBase)
            .GetMethod("RegisterActions", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(messenger, null);

        return messenger.GetActionPaths();
    }

    // ---- interfaces the core registry maps to messengers -------------------------------------------------

    [Theory]
    [InlineData(typeof(IHasCameraControls))]       // core CameraControlMessenger (replaced, see below)
    [InlineData(typeof(IHasCameraPtzControl))]
    [InlineData(typeof(IHasCameraPresets))]
    [InlineData(typeof(IHasCameraOff))]
    [InlineData(typeof(IHasCameraPrivacy))]
    [InlineData(typeof(IHasCameraFocusControl))]
    [InlineData(typeof(IHasAutoFocusMode))]
    [InlineData(typeof(IHasPowerControl))]         // core IHasPowerMessenger
    [InlineData(typeof(IHasPowerControlWithFeedback))] // core IHasPowerControlWithFeedbackMessenger
    [InlineData(typeof(ICommunicationMonitor))]    // core ICommunicationMonitorMessenger
    [InlineData(typeof(IDeviceInfoProvider))]      // core IDeviceInfoProviderMessenger
    public void Camera_Implements_The_Interface(Type contract)
    {
        Assert.True(contract.IsAssignableFrom(typeof(PanasonicCamera)), $"PanasonicCamera must implement {contract.Name}");
    }

    [Fact]
    public void Camera_Is_A_CameraBase_So_Capabilities_Are_Reported()
    {
        Assert.True(typeof(CameraBase).IsAssignableFrom(typeof(PanasonicCamera)));
    }

    // ---- replacement of core's camera messenger ----------------------------------------------------------

    [Fact]
    public void Default_Messenger_Interfaces_Exclude_Only_IHasCameraControls()
    {
        var all = typeof(PanasonicCamera).GetInterfaces();

        var defaults = PanasonicCamera.GetDefaultMessengerInterfaces(typeof(PanasonicCamera)).ToList();

        Assert.DoesNotContain(typeof(IHasCameraControls), defaults);
        Assert.Equal(all.Length - 1, defaults.Count);
        Assert.Equal(new[] { typeof(IHasCameraControls) }, all.Except(defaults));
    }

    [Theory]
    [InlineData(typeof(ICommunicationMonitor))]
    [InlineData(typeof(IHasPowerControl))]
    [InlineData(typeof(IHasPowerControlWithFeedback))]
    [InlineData(typeof(IDeviceInfoProvider))]
    public void Core_Messengers_Still_Created_For(Type contract)
    {
        Assert.Contains(contract, PanasonicCamera.GetDefaultMessengerInterfaces(typeof(PanasonicCamera)));
    }

    [Fact]
    public void Replacement_Messenger_Extends_Core_Camera_Messenger()
    {
        Assert.True(typeof(CameraControlMessenger<IHasCameraControls>).IsAssignableFrom(typeof(PanasonicCameraControlMessenger)));
    }

    [Fact]
    public void Replacement_Messenger_Keeps_Core_Camera_Actions()
    {
        var paths = ActionPaths(new PanasonicCameraControlMessenger("k", UninitializedCamera(), "/device/cam"));

        foreach (var core in new[]
        {
            "/fullStatus", "/cameraStatus",
            "/cameraUp", "/cameraDown", "/cameraLeft", "/cameraRight", "/cameraZoomIn", "/cameraZoomOut",
            "/cameraModeOff", "/cameraModeManual", "/recallPreset", "/storePreset"
        })
            Assert.Contains(core, paths);
    }

    [Fact]
    public void Replacement_Messenger_Adds_Privacy_And_Focus_Actions()
    {
        var paths = ActionPaths(new PanasonicCameraControlMessenger("k", UninitializedCamera(), "/device/cam"));

        foreach (var added in new[]
        {
            "/privacyOn", "/privacyOff",
            "/cameraFocusNear", "/cameraFocusFar", "/cameraAutoFocus",
            "/cameraFocusModeAuto", "/cameraFocusModeManual", "/cameraFocusModeToggle"
        })
            Assert.Contains(added, paths);
    }

    // ---- status message wire contract --------------------------------------------------------------------

    // DeviceMessageBase.MessageType is the runtime type name; clients look for "CameraStateMessage"
    [Fact]
    public void Status_Message_Type_Name_Matches_Core()
    {
        var json = JObject.Parse(JsonConvert.SerializeObject(new CameraStateMessage()));

        Assert.Equal("CameraStateMessage", (string?)json["messageType"]);
    }

    [Fact]
    public void Status_Message_Extends_Core_Camera_State()
    {
        Assert.True(typeof(PepperDash.Essentials.AppServer.Messengers.CameraStateMessage).IsAssignableFrom(typeof(CameraStateMessage)));
    }

    [Fact]
    public void Status_Message_Carries_Core_And_Plugin_Fields()
    {
        var message = new CameraStateMessage
        {
            CameraManualSupported = true,
            CameraOffSupported = true,
            HasPresets = true,
            CameraPrivacySupported = true,
            CameraAutoFocusOn = true,
            Capabilities = new PepperDash.Essentials.AppServer.Messengers.CameraCapabilities { CanPan = true, CanFocus = true }
        };

        var json = JObject.Parse(JsonConvert.SerializeObject(message));

        Assert.True((bool)json["cameraManualSupported"]!);
        Assert.True((bool)json["cameraOffSupported"]!);
        Assert.True((bool)json["hasPresets"]!);
        Assert.True((bool)json["cameraPrivacySupported"]!);
        Assert.True((bool)json["cameraAutoFocusOn"]!);
        Assert.True((bool)json["capabilities"]!["canFocus"]!);
    }

    [Fact]
    public void Auto_Focus_State_Is_Omitted_When_Unknown()
    {
        var json = JObject.Parse(JsonConvert.SerializeObject(new CameraStateMessage { CameraAutoFocusOn = null }));

        Assert.Null(json["cameraAutoFocusOn"]);
    }
}
