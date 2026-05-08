![PepperDash Logo](/images/logo_pdt_no_tagline_600.png)
# Panasonic Camera Plugin

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
        "titlSpeed": 25,
        "zoomSpeed": 25,
        "homeCommand": "",
        "privacyCommand": ""
    }
}
```

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


## DEVJSON Commands

When using DEVJSON commands update the program index `devjson:{programIndex}` and `deviceKey` values to match the testing environment.

```json
devjson:1 {"deviceKey":"camera-1", "methodName":"CameraOn", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"CameraOff", "params":[]}

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
devjson:1 {"deviceKey":"camera-1", "methodName":"PositionPrivacy", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"RecallPreset", "params":[4]}
devjson:1 {"deviceKey":"camera-1", "methodName":"SavePreset", "params":[9]}

devjson:1 {"deviceKey":"camera-1", "methodName":"SendCustomCommand", "params":["customCommandString"]}
```
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.8.5
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

#### Analogs

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Camera pan speed |
| 2 | R | Camera tilt speed |
| 3 | R | Camera zoom speed |
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

- IDisposable
- IKeyed
- IBridgeAdvanced
- IHasCameraPtzControl
- IHasCameraPresets
- IHasCameraOff
- ICommunicationMonitor
- IRoutingSource
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- StatusMonitorBase
- JoinMapBaseAdvanced
- CameraBase
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public string PresetRecallCommand(int preset)
- public string PresetSaveCommand(int preset)
- public void EnqueueCmd(string cmd)
- public void Dispose()
- public void PositionHome()
- public void PositionPrivacy()
- public void PanLeft()
- public void PanRight()
- public void PanStop()
- public void TiltDown()
- public void TiltUp()
- public void TiltStop()
- public void CameraOn()
- public void CameraOff()
- public void ZoomIn()
- public void ZoomOut()
- public void ZoomStop()
- public void SendCustomCommand(string cmd)
- public void PresetSelect(int preset)
- public void PresetStore(int preset, string description = null)
- public void SetIpAddress(string address)
- public void UpdatePresetName(int presetId, string name)
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void Dispose()
- public void EnqueueCmd(string path)
- public void Dispose()
- public void HandleHttpResponse(object sender, HttpResponse response)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- IsOnlineFeedback
- PresetSavedFeedback
- CameraIsOffFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- NumberOfPresetsFeedback
- PanSpeedFeedback
- ZoomSpeedFeedback
- TiltSpeedFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- NameFeedback
- ComsFb
<!-- END String Feedbacks -->
