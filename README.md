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
### Analogs

| Input      | I/O | Output               |
| ---------- | --- | -------------------- |
| Pan speed  | 1   | Pan speed fb         |
| Tilt speed | 2   | Tilt speed fb        |
| Zoom speed | 3   | Zoom speed fb        |
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
| `IDeviceInfoProvider` | Device info: host name, IP address, MAC address, serial number, firmware version (read from the camera with `cgi-bin/getinfo?FILE=1` after the first poll) |

### Camera privacy (plugin extension)

The camera implements `IHasCameraPrivacy`, which Essentials 3.0.0-rc.20 does not support in Mobile Control. The plugin adds these to the camera control messenger:

| Action / field | Description |
| -------------- | ----------- |
| `/privacyOn` | Sends the camera to its privacy position (`privacyCommand`, default `APC00000000`) |
| `/privacyOff` | Returns the camera by recalling preset 1 |
| `cameraPrivacySupported` | Added to the `CameraStateMessage` status (`/fullStatus`, `/cameraStatus`) |

The camera cannot report where it is pointed, so there is no privacy-state feedback.

### Known issue: the camera messenger is replaced, not extended in place

**Issue.** Core has no messenger for `IHasCameraPrivacy`, and its `CameraControlMessenger` cannot be extended in place:

- The Mobile Control registry creates `CameraControlMessenger` automatically for any device implementing `IHasCameraControls`. A plugin cannot add actions to that instance.
- Registering a second messenger on the same `/device/{key}` path would work for privacy, but would leave two camera messengers, and core's status message would not carry the privacy flag.
- The core status builder is private, so the extra field cannot be added by overriding a single method.

**Resolution.** The plugin overrides `CreateMobileControlMessengers()` in `PanasonicCamera`:

1. It calls `AddDefaultMessengersForDevice(this, interfaces)` with every implemented interface except `IHasCameraControls`, so core still creates the communication monitor and power messengers.
2. It registers `PanasonicCameraControlMessenger`, a subclass of core's `CameraControlMessenger<IHasCameraControls>`, under the same key core would have used (`{key}-hasCamerasWithControls-{controllerKey}`). All of core's pan, tilt, zoom, preset and power actions are inherited unchanged.
3. The subclass registers `/fullStatus` and `/cameraStatus` before calling the base, because the first handler registered for a path wins, and answers them with an extended `CameraStateMessage`. That message mirrors core's fields and adds `cameraPrivacySupported`. It keeps the type name `CameraStateMessage` because Mobile Control clients read the runtime type name as `messageType`.

**Maintenance.** The status builder mirrors a private core method, so recheck it when raising the Essentials version. When core adds `IHasCameraPrivacy` support, delete `src/MobileControlMessenger/` and the `CreateMobileControlMessengers()` override.

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

devjson:1 {"deviceKey":"camera-1", "methodName":"PositionHome", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PrivacyOn", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PrivacyOff", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"PresetSelect", "params":[4]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PresetStore", "params":[9, ""]}

devjson:1 {"deviceKey":"camera-1", "methodName":"SendCustomCommand", "params":["customCommandString"]}
```
