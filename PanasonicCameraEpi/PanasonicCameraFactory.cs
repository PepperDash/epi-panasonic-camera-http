using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;

namespace PanasonicCameraEpi
{
	public class PanasonicCameraFactory : EssentialsPluginDeviceFactory<PanasonicCamera>
	{

		public PanasonicCameraFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.8.5";

			TypeNames = new List<string> { "panasonicHttpCamera"};
        }

		public override EssentialsDevice BuildDevice(DeviceConfig config)
		{
            Debug.Console(1, "Factory Attempting to create new device from type: {0}", config.Type);
			var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);
			if (!cameraConfig.Control.Method.Equals("http", StringComparison.OrdinalIgnoreCase))
				throw new NotSupportedException("No valid control method found");

            var comms = CommFactory.CreateCommForDevice(config);
            if (comms == null)
            {
                Debug.Console(2, "[{0}] VaddioOneLink: failed to create comms for {1}", config.Key, config.Name);
                return null;
            }

            DeviceManager.AddDevice(comms);

            return new PanasonicCamera(comms, config);
		}
	}
}