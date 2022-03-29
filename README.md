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
