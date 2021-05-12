using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicCamera : ReconfigurableDevice, IHasCameraPtzControl, IHasCameraOff, ICommunicationMonitor, IBridge, IRoutingSource
    {
        private readonly StatusMonitorBase _monitor;
        private readonly PanasonicCmdBuilder _cmd;
        private readonly PanasonicResponseHandler _responseHandler;
        private readonly CommandQueue _queue;
        private readonly Dictionary<uint, PanasonicCameraPreset> _presets;

        public bool IsPoweredOn { get; private set; }
        public Dictionary<uint, StringFeedback> PresetNamesFeedbacks { get; private set; }
        public IntFeedback NumberOfPresetsFeedback { get; private set; }
        public StringFeedback NameFeedback { get; private set; }
        //public StringFeedback ComsFeedback { get; set; }
        public BoolFeedback IsOnlineFeedback { get { return _monitor.IsOnlineFeedback; } }		
        public IntFeedback PanSpeedFeedback { get; private set; }
        public IntFeedback ZoomSpeedFeedback { get; private set; }
        public IntFeedback TiltSpeedFeedback { get; private set; }

		/*
        public static void LoadPlugin()
        {
            DeviceFactory.AddFactoryForType("panasonicHttpCamera", BuildDevice);
        }

        public static PanasonicCamera BuildDevice(DeviceConfig config)
        {
            var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);
            if (!cameraConfig.Control.Method.Equals("http", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("No valid control method found");

            var client = new GenericHttpClient(string.Format("{0}-httpClient", config.Key), config.Name,
                cameraConfig.Control.TcpSshProperties.Address);

            DeviceManager.AddDevice(client);

            return new PanasonicCamera(client, config);
        }
		 */

        public PanasonicCamera(IBasicCommunication comms, DeviceConfig config)
            : base(config)
        {
            OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

			var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);

			_responseHandler = new PanasonicResponseHandler();

            if (cameraConfig.CommunicationMonitor == null)
                cameraConfig.CommunicationMonitor = new CommunicationMonitorConfig()
                {
                    PollInterval = 60000,
                    TimeToWarning = 180000,
                    TimeToError = 300000,
                    PollString = "cgi-bin/aw_ptz?cmd=%23O&res=1"
                };

            var tempClient = comms as GenericHttpClient;
			
            if(tempClient == null) 
            {
                _monitor = new GenericCommunicationMonitor(this, tempClient, cameraConfig.CommunicationMonitor);
                tempClient.TextReceived += _responseHandler.HandleResponseReceeved;
                    throw new NotImplementedException("Need to create a command queue for serial");
			}
			else
            {
                _monitor = new PanasonicHttpCameraMonitor(this, tempClient, cameraConfig.CommunicationMonitor);
                var queue = new HttpCommandQueue(comms);
                queue.ResponseReceived += _responseHandler.HandleResponseReceived;
                _queue = queue;
            }

            _cmd = new PanasonicCmdBuilder(12, 25, 12);

            _presets = cameraConfig.Presets.ToDictionary(x => (uint)x.Id);

            AddPostActivationAction(() =>
                {
                    if (cameraConfig.ZoomSpeed == 0)
                        return;

                    ZoomSpeed = cameraConfig.ZoomSpeed;
                });

            AddPostActivationAction(() =>
            {
                if (cameraConfig.TiltSpeed == 0)
                    return;

                TiltSpeed = cameraConfig.TiltSpeed;
            });

            AddPostActivationAction(() =>
            {
                if (cameraConfig.PanSpeed == 0)
                    return;

                PanSpeed = cameraConfig.PanSpeed;
            });
        }
        public override bool CustomActivate()
        {
            SetupFeedbacks();
            _responseHandler.CameraPoweredOn += (sender, args) =>
                {
                    IsPoweredOn = true;
                    CameraIsOffFeedback.FireUpdate();
                };

            _responseHandler.CameraPoweredOff += (sender, args) =>
                {
                    IsPoweredOn = false;
                    CameraIsOffFeedback.FireUpdate();
                };

            _monitor.StatusChange += HandleMonitorStatusChange;
            _monitor.Start();

            return true;
        }

        private void HandleMonitorStatusChange(object sender, MonitorStatusChangeEventArgs e)
        {
            Debug.Console(1, this, "STATUS: '{0}'", e.Message);
        }

        private void SetupFeedbacks()
        {
            NameFeedback = new StringFeedback(() => Name);
            NameFeedback.FireUpdate();

            for (uint x = 1; x <= 10; x++)
            {
                var index = x;
                if (_presets.ContainsKey(index))
                    continue;

                _presets.Add(index, new PanasonicCameraPreset() {Id = (int) index, Name = String.Empty});
            }

            NumberOfPresetsFeedback = new IntFeedback(() => _presets.Values.Count(x => !String.IsNullOrEmpty(x.Name)));
            NumberOfPresetsFeedback.FireUpdate();

            PanSpeedFeedback = new IntFeedback(() => PanSpeed);
            TiltSpeedFeedback = new IntFeedback(() => TiltSpeed);
            ZoomSpeedFeedback = new IntFeedback(() => ZoomSpeed);

            PanSpeedFeedback.FireUpdate();
            TiltSpeedFeedback.FireUpdate();
            ZoomSpeedFeedback.FireUpdate();

            PresetNamesFeedbacks = _presets.ToDictionary(x => x.Key, x => new StringFeedback(() => x.Value.Name));

            foreach (var feedback in PresetNamesFeedbacks)
                feedback.Value.FireUpdate();

            CameraIsOffFeedback = new BoolFeedback(() => !IsPoweredOn);
            CameraIsOffFeedback.FireUpdate(); 
        }

        public int PanSpeed
        {
            get { return _cmd.PanSpeed; }
            set 
            { 
                _cmd.PanSpeed = value;
                PanSpeedFeedback.FireUpdate();
            }
        }

        public int ZoomSpeed 
        {
            get { return _cmd.ZoomSpeed; }
            set 
            { 
                _cmd.ZoomSpeed = value;
                ZoomSpeedFeedback.FireUpdate();
            }
        }

        public int TiltSpeed
        {
            get { return _cmd.TiltSpeed; }
            set 
            { 
                _cmd.TiltSpeed = value;
                TiltSpeedFeedback.FireUpdate();
            }
        }

        #region IHasCameraPtzControl Members

        public void PositionHome()
        {
            _queue.EnqueueCmd(_cmd.HomeCommand);
        }

        public void PositionPrivacy()
        {
            _queue.EnqueueCmd(_cmd.PrivacyCommand);
        }

        #endregion

        #region IHasCameraPanControl Members

        public void PanLeft()
        {
            _queue.EnqueueCmd(_cmd.PanLeftCommand);
        }

        public void PanRight()
        {
            _queue.EnqueueCmd(_cmd.PanRightCommand);
        }

        public void PanStop()
        {
            _queue.EnqueueCmd(_cmd.PanStopCommand);
        }

        #endregion

        #region IHasCameraTiltControl Members

        public void TiltDown()
        {
            _queue.EnqueueCmd(_cmd.TiltDownCommand);			
        }

        public void TiltUp()
        {
            _queue.EnqueueCmd(_cmd.TiltUpCommand);
        }

        public void TiltStop()
        {
            _queue.EnqueueCmd(_cmd.TiltStopCommand);
        }    

        #endregion

        #region IHasCameraOff Members

        public BoolFeedback CameraIsOffFeedback { get; private set; }

        public void CameraOn()
        {
            _queue.EnqueueCmd(_cmd.PowerOnCommand);
        }

        public void CameraOff()
        {
            _queue.EnqueueCmd(_cmd.PowerOffCommand);
        }

        #endregion

        #region IHasCameraZoomControl Members

        public void ZoomIn()
        {
            _queue.EnqueueCmd(_cmd.ZoomInCommand);
        }

        public void ZoomOut()
        {
            _queue.EnqueueCmd(_cmd.ZoomOutCommand);		
        }

        public void ZoomStop()
        {
            _queue.EnqueueCmd(_cmd.ZoomStopCommand);
        }

        #endregion

        public void SendCustomCommand(string cmd)
        {
            _queue.EnqueueCmd(PanasonicCmdBuilder.BuildCustomCommand(cmd));
        }

        public void RecallPreset(int preset)
        {
            _queue.EnqueueCmd(_cmd.PresetRecallCommand(preset));	        
        }

        public void SavePreset(int preset)
        {
            _queue.EnqueueCmd(_cmd.PresetSaveCommand(preset));
        }

		/// <summary>
		/// Sets the IP address used by the plugin 
		/// </summary>
		/// <param name="hostname">string</param>
		public void SetIpAddress(string address)
		{
			try
			{
				if (address.Length > 2 & Config.Properties["control"]["tcpSshProperties"]["address"].ToString() != address)
				{
					Debug.Console(2, this, "Changing IPAddress: {0}", address);

					Config.Properties["control"]["tcpSshProperties"]["address"] = address;
					Debug.Console(2, this, "{0}", Config.Properties.ToString());
					CustomSetConfig(Config);
					var tempClient = DeviceManager.GetDeviceForKey(string.Format("{0}-httpClient", this.Key)) as GenericHttpClient;
					tempClient.Client.HostName = address;
				}
			}
			catch (Exception e)
			{
				if (Debug.Level == 2)
					Debug.Console(2, this, "Error SetIpAddress: '{0}'", e);
			}
		}

	
        public void UpdatePresetName(int presetId, string name)
        {
            if (String.IsNullOrEmpty(name))
                return;

            PanasonicCameraPreset preset;
            if (!_presets.TryGetValue((uint)presetId, out preset))
                throw new ArgumentException("preset id does not exist");

            preset.Name = name;

            foreach (var feedback in PresetNamesFeedbacks)
                feedback.Value.FireUpdate();
            
            NumberOfPresetsFeedback.FireUpdate();

            var props = PanasonicCameraPropsConfig.FromDeviceConfig(Config);
            props.Presets = _presets.Values.ToList();

            Config.Properties = JObject.FromObject(props);
            SetConfig(Config);
        }

        #region IBridge Members

        public void LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            this.LinkToApiExt(trilist, joinStart, joinMapKey);
        }

        #endregion

        #region ICommunicationMonitor Members

        public StatusMonitorBase CommunicationMonitor
        {
            get { return _monitor; }
        }

        #endregion

        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }
    }
}