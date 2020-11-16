using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;
using System.Collections.Generic;

namespace PanasonicCameraEpi
{
    public class DeviceFactory : EssentialsPluginDeviceFactory<PanasonicCamera>
    {
        /// <summary>
        /// Factory for building new Panasonic Camera Device
        /// </summary>
        public DeviceFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.6.3";

            TypeNames = new List<string> {"panasonicHttpCamera"};
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new Panasonic Camera Device");

            var comm = CommFactory.CreateCommForDevice(dc);

            return new PanasonicCamera(comm, dc);
        }

    }

}