using System.Linq;
using Crestron.SimplSharp.Reflection;

using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicCameraJoinMap : JoinMapBaseAdvanced
    {
        public PanasonicCameraJoinMap(uint joinStart)
            : base(joinStart, typeof (PanasonicCameraJoinMap))
        {
            
        }

        [JoinName("TiltUp")]
        public JoinDataComplete TiltUp =
            new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Tilt Camera Up",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("TiltDown")]
        public JoinDataComplete TiltDown =
            new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Tilt Camera Down",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PanLeft")]
        public JoinDataComplete PanLeft =
            new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Pan Camera Left",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PanRight")]
        public JoinDataComplete PanRight =
            new JoinDataComplete(new JoinData { JoinNumber = 4, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Pan Camera Right",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ZoomIn")]
        public JoinDataComplete ZoomIn =
            new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Zoom Camera In",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ZoomOut")]
        public JoinDataComplete ZoomOut =
            new JoinDataComplete(new JoinData { JoinNumber = 6, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Zoom Camera Out",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PowerOn")]
        public JoinDataComplete PowerOn =
            new JoinDataComplete(new JoinData { JoinNumber = 7, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Power Camera On",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PowerOff")]
        public JoinDataComplete PowerOff =
            new JoinDataComplete(new JoinData { JoinNumber = 8, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Power Camera On",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline =
            new JoinDataComplete(new JoinData { JoinNumber = 9, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Camera is Online",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Home")]
        public JoinDataComplete Home =
            new JoinDataComplete(new JoinData { JoinNumber = 10, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Recall Camera Home Preset",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresetRecall")]
        public JoinDataComplete PresetRecall =
            new JoinDataComplete(new JoinData { JoinNumber = 11, JoinSpan = 16 },
            new JoinMetadata
            {
                Description = "Preset Recall",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresetSave")]
        public JoinDataComplete PresetSave =
            new JoinDataComplete(new JoinData { JoinNumber = 31, JoinSpan = 16 },
            new JoinMetadata
            {
                Description = "Preset Save",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PrivacyOn")]
        public JoinDataComplete PrivacyOn =
            new JoinDataComplete(new JoinData { JoinNumber = 48, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Enable Camera Privacy",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PrivacyOff")]
        public JoinDataComplete PrivacyOff =
            new JoinDataComplete(new JoinData { JoinNumber = 49, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Disable Camera Privacy",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PanSpeed")]
        public JoinDataComplete PanSpeed =
            new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Set Pan Speed",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("TiltSpeed")]
        public JoinDataComplete TiltSpeed =
            new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Set Tilt Speed",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("ZoomSpeed")]
        public JoinDataComplete ZoomSpeed =
            new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Set Zpp, Speed",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("NumberOfPresets")]
        public JoinDataComplete NumberOfPresets =
            new JoinDataComplete(new JoinData { JoinNumber = 4, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Get Number of Presets",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("DeviceName")]
        public JoinDataComplete DeviceName =
            new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Camera Name",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("PresetName")]
        public JoinDataComplete PresetName =
            new JoinDataComplete(new JoinData { JoinNumber = 11, JoinSpan = 16 },
            new JoinMetadata
            {
                Description = "Camera Preset Name",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DeviceComs")]
        public JoinDataComplete DeviceComs =
            new JoinDataComplete(new JoinData { JoinNumber = 50, JoinSpan = 1 },
            new JoinMetadata
            {
                Description = "Camera Com Passthrough",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });


    }

}