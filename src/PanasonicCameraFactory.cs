using System;
using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

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
			var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);
			if (!cameraConfig.Control.Method.Equals("http", StringComparison.OrdinalIgnoreCase))
				throw new NotSupportedException("No valid control method found");

			// No longer need to create GenericHttpClient - PanasonicCamera will handle HTTP directly
			// Create a dummy communication object if needed, or pass null
			return new PanasonicCamera(null, config);
		}
	}
}