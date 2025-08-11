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

			var client = new GenericHttpClient(string.Format("{0}-httpClient", config.Key), config.Name,
				cameraConfig.Control.TcpSshProperties.Address);

			DeviceManager.AddDevice(client);

			return new PanasonicCamera(client, config);
		}
	}
}