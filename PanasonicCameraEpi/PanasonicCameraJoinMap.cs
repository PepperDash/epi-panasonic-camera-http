using System.Linq;
using Crestron.SimplSharp.Reflection;

using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicCameraBridgeJoinMap : JoinMapBaseAdvanced
    {
        #region Digital

		[JoinName("TiltUp")]
		public JoinDataComplete TiltUp = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Tilt Up",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("TiltDown")]
		public JoinDataComplete TiltDown = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Tilt Down",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PanLeft")]
		public JoinDataComplete PanLeft = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 3,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Pan Left",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PanRight")]
		public JoinDataComplete PanRight = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 4,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Pan Right",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ZoomIn")]
		public JoinDataComplete ZoomIn = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 5,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Zoom In",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("ZoomOut")]
		public JoinDataComplete ZoomOut = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 6,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Zoom Out",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PowerOn")]
		public JoinDataComplete PowerOn = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 7,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Power On",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PowerOff")]
		public JoinDataComplete PowerOff = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 8,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Power Off",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 9,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Is Online",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("Home")]
		public JoinDataComplete Home = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 10,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Home",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PresetRecall")]
		public JoinDataComplete PresetRecall = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 11,
				JoinSpan = 16
			},
			new JoinMetadata()
			{
				Description = "Preset Recall",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

        [JoinName("FocusNear")]
        public JoinDataComplete FocusNear = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 28,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "FocusNear",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FocusFar")]
        public JoinDataComplete FocusFar = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 29,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "FocusFar",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

		[JoinName("AutoFocus")]
		public JoinDataComplete AutoFocus = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 30,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "AutoFocus",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PresetSave")]
		public JoinDataComplete PresetSave = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 31,
				JoinSpan = 16
			},
			new JoinMetadata()
			{
				Description = "Preset Save",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PrivacyOn")]
		public JoinDataComplete PrivacyOn = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 48,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Privacy On",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		[JoinName("PrivacyOff")]
		public JoinDataComplete PrivacyOff = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 49,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Privacy Off",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		#endregion


		#region Analog

		[JoinName("TiltSpeed")]
		public JoinDataComplete TiltSpeed = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Tilt Speed",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Analog
			});

		[JoinName("PanSpeed")]
		public JoinDataComplete PanSpeed = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Pan Speed",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Analog
			});

		[JoinName("ZoomSpeed")]
		public JoinDataComplete ZoomSpeed = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 3,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Zoom Speed",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Analog
			});


		[JoinName("PresetCount")]
		public JoinDataComplete PresetCount = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 11,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Preset Count",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		[JoinName("Status")]
		public JoinDataComplete Status = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 50,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Status",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		#endregion


		#region Serial

		[JoinName("DeviceName")]
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata()
			{
				Description = "Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("PresetName")]
		public JoinDataComplete PresetNames = new JoinDataComplete(
			new JoinData()
			{
				JoinNumber = 11,
				JoinSpan = 16
			},
			new JoinMetadata()
			{
				Description = "Preset Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="joinStart"></param>
        public PanasonicCameraBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(PanasonicCameraBridgeJoinMap))
		{
		}
	}
}