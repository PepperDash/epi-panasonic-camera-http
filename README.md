![PepperDash Logo](/images/logo_pdt_no_tagline_600.png)
# Panasonic Camera Plugin

Requires **PepperDash Essentials 3.0.0 or higher** (.NET 8, Crestron 4-Series processors). For Essentials 2.x use a 1.x release of this plugin.

Controls the following cameras:

1. AW-HE20
1. AW-HE120
1. AW-HE60
1. AW-HE130
1. AW-HE70
1. AW-HE40
1. AW-SFU01
1. AK-UB300
1. AW-HR140
1. AW-UE150
1. AK-UB300
1. AW-UE150
1. AW-HE42

[Control Documentation](https://eww.pass.panasonic.co.jp/pro-av/support/content/guide/DEF/HE50_120_IP/HDIntegratedCamera_InterfaceSpecifications-E.pdf)

## Essentials Device Configuration

```json
{
    "key": "camera-1",
    "name": "Camera 1",
    "type": "panasonicHttpCamera",
    "group": "plugin",
    "properties": {
        "control": {
        "method": "http",
        "tcpSshProperties": {
                "address": "10.120.17.69",
                "port": 80
            }
        },
        "presets": [
            {
                "Name": "Preset 1",
                "Id": 1
            },
            {
                "Name": "Preset 2",
                "Id": 2
            },
            {
                "Name": "Preset 3",
                "Id": 3
            }
        ],
        "panSpeed": 25,
        "tiltSpeed": 25,
        "zoomSpeed": 25,
        "focusSpeed": 25,
        "pacing": 130,
        "homeCommand": "",
        "privacyCommand": ""
    }
}
```

`pacing` (optional, milliseconds, default `130`) is the minimum gap between the start of consecutive commands sent to the camera. 130 ms is the value recommended in the Panasonic interface specification.

## Essentials Bridging

```json
{
    "key": "plugin-bridge-1",
    "name": "Plugin Bridge",
    "group": "api",
    "type": "eiscApiAdvanced",
    "properties": {
        "control": {
            "tcpSshProperties": {
                "address": "127.0.0.2",
                "port": 0
            },
            "ipid": "B2",
            "method": "ipidTcp"
        },
        "devices": [
            {
                "deviceKey": "camera-1",
                "joinStart": 1
            }
        ]
    }
}
```

## Essentials Bridge Join Map

The join map below documents the commands implemented in this plugin.

### Digitals

| Input            | I/O | Output       |
| ---------------- | --- | ------------ |
| Tilt up          | 1   |              |
| Tilt down        | 2   |              |
| Pan left         | 3   |              |
| Pan right        | 4   |              |
| Zoom in          | 5   |              |
| Zoom out         | 6   |              |
| Power on         | 7   | Power on Fb  |
| Power off        | 8   | Power off fb |
|                  | 9   | Is online fb |
| Home             | 10  |              |
| Preset 1 recall  | 11  |              |
| Preset 2 recall  | 12  |              |
| Preset 15 recall | 25  |              |
| Preset 16 recall | 26  |              |
| Preset 1 save    | 31  |              |
| Preset 2 save    | 32  |              |
| Preset 15 save   | 45  |              |
| Preset 16 save   | 46  |              |
| Privacy on       | 48  |              |
| Privacy off      | 49  |              |
| Focus near       | 50  |              |
| Focus far        | 51  |              |
| One-touch auto focus | 52 |           |
| Auto focus on    | 53  | Auto focus on fb  |
| Auto focus off   | 54  | Auto focus off fb |
### Analogs

| Input      | I/O | Output               |
| ---------- | --- | -------------------- |
| Pan speed  | 1   | Pan speed fb         |
| Tilt speed | 2   | Tilt speed fb        |
| Zoom speed | 3   | Zoom speed fb        |
| Focus speed | 4  | Focus speed fb       |
|            | 11  | Number of presets fb |

### Serials

| Input                | I/O | Output                  |
| -------------------- | --- | ----------------------- |
|                      | 1   | Device name fb          |
| IP address           | 2   | IP address fb           |
|                      | 11  | Preset 1 name fb        |
|                      | 12  | Preset 2 name fb        |
|                      | 25  | Preset 15 name fb       |
|                      | 26  | Preset 16 name fb       |
| Device communication | 50  | Device communication fb |


## Mobile Control

When an Essentials Mobile Control controller is present, the camera is exposed at `/device/{deviceKey}`. The core messengers are created from the interfaces the camera implements:

| Interface | Messenger |
| --------- | --------- |
| `ICommunicationMonitor` | Communication monitor status |
| `IHasCameraPtzControl` / `IHasCameraPresets` / `IHasCameraOff` | Camera control: pan, tilt, zoom, preset recall and store, camera status and capabilities (**extended by this plugin**, see below) |
| `IHasPowerControl` / `IHasPowerControlWithFeedback` | Power commands and power state |
| `IDeviceInfoProvider` | Device info: host name, IP address, MAC address, serial number, firmware version (read from the camera with `cgi-bin/getinfo?FILE=1` after the first poll; this command is not in the interface specification and has not been verified on hardware) |

### Privacy and focus (plugin extensions)

The camera implements `IHasCameraPrivacy`, `IHasCameraFocusControl` and `IHasAutoFocusMode`, which Essentials 3.0.0-rc.20 does not support in Mobile Control. The plugin adds these to the camera control messenger:

| Action / field | Description |
| -------------- | ----------- |
| `/privacyOn` | Sends the camera to its privacy position (`privacyCommand`, default `APC00000000`) |
| `/privacyOff` | Returns the camera by recalling preset 1 |
| `/cameraFocusNear`, `/cameraFocusFar` | Press-and-hold focus (same payload as core's `/cameraUp`, `/cameraZoomIn`, ...) |
| `/cameraAutoFocus` | One-touch auto focus |
| `/cameraFocusModeAuto`, `/cameraFocusModeManual`, `/cameraFocusModeToggle` | Auto focus mode on, off, toggle |
| `cameraPrivacySupported`, `cameraAutoFocusOn` | Added to the `CameraStateMessage` status (`/fullStatus`, `/cameraStatus`); `cameraAutoFocusOn` is also pushed when it changes. `capabilities.canFocus` is true |

The camera cannot report where it is pointed, so there is no privacy-state feedback.

Manual focus is ignored by the camera while auto focus is on (it replies `ER3`), and the focus speed is set with `focusSpeed` (1-49, default 25). The Mobile Control React app core (`useCameraBase`) does not yet send focus actions, so a UI needs to add them.

### Known issue: the camera messenger is replaced, not extended in place

**Issue.** Core has no messenger for `IHasCameraPrivacy` or focus, and its `CameraControlMessenger` cannot be extended in place:

- The Mobile Control registry creates `CameraControlMessenger` automatically for any device implementing `IHasCameraControls`. A plugin cannot add actions to that instance.
- Registering a second messenger on the same `/device/{key}` path would work for these actions, but would leave two camera messengers, and core's status message would not carry the new fields.
- The core status builder is private, so the extra field cannot be added by overriding a single method.

**Resolution.** The plugin overrides `CreateMobileControlMessengers()` in `PanasonicCamera`:

1. It calls `AddDefaultMessengersForDevice(this, interfaces)` with every implemented interface except `IHasCameraControls`, so core still creates the communication monitor and power messengers.
2. It registers `PanasonicCameraControlMessenger`, a subclass of core's `CameraControlMessenger<IHasCameraControls>`, under the same key core would have used (`{key}-hasCamerasWithControls-{controllerKey}`). All of core's pan, tilt, zoom, preset and power actions are inherited unchanged.
3. The subclass registers `/fullStatus` and `/cameraStatus` before calling the base, because the first handler registered for a path wins, and answers them with an extended `CameraStateMessage`. That message mirrors core's fields and adds `cameraPrivacySupported` and `cameraAutoFocusOn`. It keeps the type name `CameraStateMessage` because Mobile Control clients read the runtime type name as `messageType`.

**Maintenance.** The status builder mirrors a private core method, so recheck it when raising the Essentials version. When core adds `IHasCameraPrivacy` support, delete `src/MobileControlMessenger/` and the `CreateMobileControlMessengers()` override.

## Tests

```
dotnet test tests/PepperDash.Essentials.Plugins.Panasonic.Camera.Http.Tests.csproj
```

The suite runs without a processor or a camera:

- **Plugin loading** (MetadataLoadContext): the assembly loads, the package id matches the repo-derived name CI enforces, the factory is discoverable, and its `TypeNames` and `MinimumEssentialsFrameworkVersion` are valid.
- **Config contract**: the config class matches `exampleConfig.json`, and real JSON deserializes.
- **Commands and replies**: command strings are checked against the interface specification, and power, auto focus and device info replies are parsed.
- **HTTP queue**: ordering and pacing are checked against a local HTTP listener.
- **Mobile Control**: the interfaces the core messengers are created from, the replaced camera messenger's actions, and the `CameraStateMessage` wire format.

`PanasonicCamera` itself needs a processor to construct, so its activation and the messenger registration against a live Mobile Control controller are not covered.

## DEVJSON Commands

When using DEVJSON commands update the program index `devjson:{programIndex}` and `deviceKey` values to match the testing environment.

```json
devjson:1 {"deviceKey":"camera-1", "methodName":"PowerOn", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PowerOff", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"PanLeft", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PanRight", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PanStop", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"TiltUp", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"TiltDown", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"TiltStop", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"ZoomIn", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"ZoomOut", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"ZoomStop", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"FocusNear", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"FocusFar", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"FocusStop", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"TriggerAutoFocus", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"SetFocusModeAuto", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"SetFocusModeManual", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"PositionHome", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PrivacyOn", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PrivacyOff", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"PresetSelect", "params":[4]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PresetStore", "params":[9, ""]}

devjson:1 {"deviceKey":"camera-1", "methodName":"SendCustomCommand", "params":["customCommandString"]}
```
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 3.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "PanasonicCameraProps",
    "group": "Group",
    "properties": {
        "control": {
            "Method": "SampleString",
            "TcpSshProperties": {
                "Address": "SampleString",
                "Port": 0
            }
        },
        "communicationMonitor": "SampleValue",
        "presets": [
            {
                "Name": "SampleString",
                "Id": 0
            }
        ],
        "PanSpeed": 0,
        "ZoomSpeed": 0,
        "TiltSpeed": 0,
        "FocusSpeed": 0,
        "HomeCommand": "SampleString",
        "PrivacyCommand": "SampleString",
        "Pacing": 0
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Camera tilt up |
| 2 | R | Camera tilt down |
| 3 | R | Camera pan left |
| 4 | R | Camera pan right |
| 5 | R | Camera zoom in |
| 6 | R | Camera zoom out |
| 7 | R | Camera power on |
| 8 | R | Camera power off |
| 9 | R | Camera is online |
| 10 | R | Camera home |
| 11 | R | Camera preset recall |
| 30 | R | Camera preset saved Feedback |
| 31 | R | Camera preset save |
| 48 | R | Camera privacy on |
| 49 | R | Camera privacy off |
| 50 | R | Camera focus near |
| 51 | R | Camera focus far |
| 52 | R | Camera one-touch auto focus |
| 53 | R | Camera auto focus on |
| 54 | R | Camera auto focus off |

#### Analogs

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Camera pan speed |
| 2 | R | Camera tilt speed |
| 3 | R | Camera zoom speed |
| 4 | R | Camera focus speed |
| 11 | R | Camera number of preset |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Camera device name |
| 2 | R | Camera IP address |
| 11 | R | Camera preset names |
| 50 | R | Camera device communications |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IKeyed
- IDisposable
- IBridgeAdvanced
- IHasCameraPtzControl
- IHasCameraFocusControl
- IHasAutoFocusMode
- IHasCameraPresets
- IHasCameraOff
- IHasCameraPrivacy
- IHasPowerControlWithFeedback
- ICommunicationMonitor
- IRoutingSource
- IDeviceInfoProvider
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- CameraBase
- JoinMapBaseAdvanced
- EventArgs
- StatusMonitorBase
- CameraControlMessenger<IHasCameraControls>
- PepperDash.Essentials.AppServer.Messengers.CameraStateMessage
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void EnqueueCmd(string path)
- public void Dispose()
- public void PositionHome()
- public void PanLeft()
- public void PanRight()
- public void PanStop()
- public void TiltDown()
- public void TiltUp()
- public void TiltStop()
- public void CameraOff()
- public void PowerOn()
- public void PowerOff()
- public void PowerToggle()
- public void PrivacyOn()
- public void PrivacyOff()
- public void ZoomIn()
- public void ZoomOut()
- public void ZoomStop()
- public void FocusNear()
- public void FocusFar()
- public void FocusStop()
- public void TriggerAutoFocus()
- public void SetFocusModeAuto()
- public void SetFocusModeManual()
- public void ToggleFocusMode()
- public void SendCustomCommand(string cmd)
- public void PresetSelect(int preset)
- public void PresetStore(int preset, string description)
- public void SetIpAddress(string address)
- public void UpdatePresetName(int presetId, string name)
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void UpdateDeviceInfo()
- public void Dispose()
- public void HandleHttpResponse(object sender, HttpResponse response)
- public string PresetRecallCommand(int preset)
- public string PresetSaveCommand(int preset)
- public void Camera_Implements_The_Interface(Type contract)
- public void Camera_Is_A_CameraBase_So_Capabilities_Are_Reported()
- public void Default_Messenger_Interfaces_Exclude_Only_IHasCameraControls()
- public void Core_Messengers_Still_Created_For(Type contract)
- public void Replacement_Messenger_Extends_Core_Camera_Messenger()
- public void Replacement_Messenger_Keeps_Core_Camera_Actions()
- public void Replacement_Messenger_Adds_Privacy_And_Focus_Actions()
- public void Status_Message_Type_Name_Matches_Core()
- public void Status_Message_Extends_Core_Camera_State()
- public void Status_Message_Carries_Core_And_Plugin_Fields()
- public void Auto_Focus_State_Is_Omitted_When_Unknown()
- public void Fixed_Commands(string property, string expected)
- public void Configured_Home_And_Privacy_Commands_Override_The_Defaults()
- public void Pan_Tilt_Zoom_Commands_Are_Centered_On_50()
- public void Preset_Recall_Is_Zero_Based_And_Padded(int preset, string expected)
- public void Preset_Save_Is_Zero_Based_And_Padded(int preset, string expected)
- public void Focus_Commands_Default_Speed()
- public void Focus_Speed_Sets_Near_And_Far(int speed, string near, string far)
- public void Slowest_Speeds_Are_Zero_Padded_To_Two_Digits()
- public void Invalid_Focus_Speed_Falls_Back_To_25(int speed)
- public void Auto_Focus_Commands()
- public void Assembly_Loads_Successfully()
- public void Assembly_Name_Matches_Expected()
- public void Assembly_Name_Matches_Repo_Derived_Package_Name()
- public void Factory_Count_Matches_Expected()
- public void Factory_Exists_ByName(string factoryName)
- public void All_Factories_Have_Parameterless_Constructor()
- public void Factory_Builds_The_Camera_Device_Type()
- public void Config_Class_Exists()
- public void Config_Has_Parameterless_Constructor()
- public void Config_Exposes_Property(string name)
- public void Config_Numeric_Property_Is_Int(string name)
- public void Config_Property_Has_JsonPropertyAttribute(string jsonName)
- public void Power_Reply_P1_Raises_PoweredOn()
- public void Power_Reply_P0_Raises_PoweredOff()
- public void Auto_Focus_Reply_Raises_AutoFocusChanged(string content, bool expected)
- public void Unrelated_Replies_Raise_Nothing(string content)
- public void Device_Info_Reply_Is_Parsed()
- public void Device_Info_Reply_Is_Not_Treated_As_A_Power_Reply()
- public void Latest_Reply_Is_Exposed_As_Coms_Feedback()
- public void Factory_Sets_MinimumEssentialsFrameworkVersion()
- public void MinimumEssentialsFrameworkVersion_Is_Plain_Numeric_And_Matches_Pinned_Essentials()
- public void Factory_Sets_TypeNames()
- public void Factory_Source_Contains_TypeName(string typeName)
- public void No_Duplicate_TypeNames()
- public void Full_Config_Deserializes()
- public void Missing_Optional_Values_Default_To_Zero_So_The_Plugin_Defaults_Apply()
- public void Example_Config_Deserializes()
- public void Every_Example_Config_Key_Maps_To_A_Config_Property()
- public void Example_Config_Poll_String_Is_A_Real_Camera_Command()
- public void Dispose()
- public void Default_Pacing_Is_The_Spec_Recommended_130ms()
- public void Enqueue_After_Dispose_Is_Ignored()
- public void Dispose_Twice_Is_Safe()
- public void Empty_Path_Is_Rejected()
- public void Empty_Hostname_Is_Rejected(string? hostname)
- public void Key_Uses_The_Parent_Device_Key()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- IsOnlineFeedback
- AutoFocusIsOnFeedback
- PresetSavedFeedback
- CameraIsOffFeedback
- PowerIsOnFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- NumberOfPresetsFeedback
- PanSpeedFeedback
- ZoomSpeedFeedback
- TiltSpeedFeedback
- FocusSpeedFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- NameFeedback
- ComsFb
<!-- END String Feedbacks -->
