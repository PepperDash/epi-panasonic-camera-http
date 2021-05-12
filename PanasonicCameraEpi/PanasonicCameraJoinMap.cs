using System.Linq;
using Crestron.SimplSharp.Reflection;

using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicCameraJoinMap : JoinMapBase
    {
        //digitals
        public uint TiltUp { get; private set; }
        public uint TiltDown { get; private set; }
        public uint PanLeft { get; private set; }
        public uint PanRight { get; private set; }
        public uint ZoomIn { get; private set; }
        public uint ZoomOut { get; private set; }
        public uint PowerOn { get; private set; }
        public uint PowerOff { get; private set; }
        public uint IsOnline { get; private set; }
		public uint Home { get; private set; }
        public uint PresetRecallStart { get; private set; }
        public uint PresetSaveStart { get; private set; }
        public uint PrivacyOn { get; private set; }
        public uint PrivacyOff { get; private set; }
        
        //analogs
        public uint PanSpeed { get; private set; }
        public uint TiltSpeed { get; private set; }
        public uint ZoomSpeed { get; private set; }
        public uint NumberOfPresets { get; private set; }

        //serial
        public uint DeviceName { get; private set; }
		public uint IPAddress { get; private set; }
        public uint PresetNameStart { get; private set; }
        public uint DeviceComs { get; private set; }


        public PanasonicCameraJoinMap()
        {
            TiltUp = 1;
            TiltDown = 2;
            PanLeft = 3;
            PanRight = 4;
            ZoomIn = 5;
            ZoomOut = 6;
            PowerOn = 7;
            PowerOff = 8;
            IsOnline = 9;
	        Home = 10;
            PresetRecallStart = 11;
            PresetSaveStart = 31;
            PrivacyOn = 48;
            PrivacyOff = 49;

            PanSpeed = 1;
            TiltSpeed = 2;
            ZoomSpeed = 3;
            NumberOfPresets = 11;
 
            DeviceName = 1;
			IPAddress = 2; 
			PresetNameStart = 11;
            DeviceComs = 50;
        }

        public override void OffsetJoinNumbers(uint joinStart)
        {
            GetType()
                .GetCType()
                .GetProperties()
                .Where(x => x.PropertyType == typeof(uint))
                .ToList()
                .ForEach(prop => prop.SetValue(this, (uint)prop.GetValue(this, null) + joinStart - 1, null));
        }
    }
}