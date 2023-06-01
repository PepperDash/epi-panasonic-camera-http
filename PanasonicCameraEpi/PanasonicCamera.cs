using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Core;
using Crestron.SimplSharp;


namespace PanasonicCameraEpi
{
    public class PanasonicCamera : ReconfigurableDevice, IBridgeAdvanced, IHasCameraPtzControl, IHasCameraOff, ICommunicationMonitor, IRoutingSource
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
        public BoolFeedback PresetSavedFeedback { get; private set; }
        public bool PresetSavedBool
        {
            get
            {
                return _PresetSavedBool;
            }
            set
            {
                _PresetSavedBool = value;
                PresetSavedFeedback.FireUpdate();
            }

               
        }
        private bool _PresetSavedBool { get; set; }

        public PanasonicCamera(IBasicCommunication comms, DeviceConfig config)
            : base(config)
        {
            OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

			var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);

			_responseHandler = new PanasonicResponseHandler();

            if (cameraConfig.CommunicationMonitor == null)
                cameraConfig.CommunicationMonitor = new CommunicationMonitorConfig
                {
                    PollInterval = 60000,
                    TimeToWarning = 180000,
                    TimeToError = 300000,
                    PollString = "cgi-bin/aw_ptz?cmd=%23O&res=1"
                };

            var tempClient = comms as GenericHttpClient;	
            if(tempClient == null) 
            {
                _monitor = new GenericCommunicationMonitor(this, comms, cameraConfig.CommunicationMonitor);
                comms.TextReceived += _responseHandler.HandleResponseReceeved;
                    throw new NotImplementedException("Need to create a command queue for serial");
			}
            _monitor = new PanasonicHttpCameraMonitor(this, tempClient, cameraConfig.CommunicationMonitor);
            HttpCommandQueue queue; 
            if (cameraConfig.pacing > 0)
            {
                 queue = new HttpCommandQueue(comms, cameraConfig.pacing);
            }
            else
            {
                 queue = new HttpCommandQueue(comms);
            }
            queue.ResponseReceived += _responseHandler.HandleResponseReceived;
            _queue = queue;

            _cmd = new PanasonicCmdBuilder(12, 25, 12, cameraConfig.HomeCommand, cameraConfig.PrivacyCommand);
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

                _presets.Add(index, new PanasonicCameraPreset {Id = (int) index, Name = String.Empty});
            }

            NumberOfPresetsFeedback = new IntFeedback(() => _presets.Values.Count(x => !String.IsNullOrEmpty(x.Name)));
            NumberOfPresetsFeedback.FireUpdate();

            PanSpeedFeedback = new IntFeedback(() => PanSpeed);
            TiltSpeedFeedback = new IntFeedback(() => TiltSpeed);
            ZoomSpeedFeedback = new IntFeedback(() => ZoomSpeed);
            PresetSavedFeedback = new BoolFeedback(() => PresetSavedBool); 

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
            if (!IsPoweredOn)
                _queue.EnqueueCmd(_cmd.PowerOnCommand);

            _queue.EnqueueCmd(_cmd.PresetRecallCommand(preset));	        
        }

        public void SavePreset(int preset)
        {
            _queue.EnqueueCmd(_cmd.PresetSaveCommand(preset));
            PresetSavedBool = true;
            new CTimer( (o) => PresetSavedBool = false, 5000);
        }

		/// <summary>
		/// Sets the IP address used by the plugin 
		/// </summary>
        /// <param name="address">string</param>
		public void SetIpAddress(string address)
		{
			try
			{
			    if (!(address.Length > 2 & Config.Properties["control"]["tcpSshProperties"]["address"].ToString() != address))
			        return;
			    Debug.Console(2, this, "Changing IPAddress: {0}", address);

			    Config.Properties["control"]["tcpSshProperties"]["address"] = address;
			    Debug.Console(2, this, "{0}", Config.Properties.ToString());
			    SetConfig(Config);
			    var tempClient = DeviceManager.GetDeviceForKey(string.Format("{0}-httpClient", Key)) as GenericHttpClient;
			    if (tempClient == null)
			    {
			        throw new Exception("Error - No Valid TCP Client!");
			    }
			    tempClient.Client.HostName = address;
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

        #region Overrides of EssentialsBridgeableDevice

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>        
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new PanasonicCameraBridgeJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            // links to bridge
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
            trilist.SetStringSigAction(joinMap.DeviceComs.JoinNumber, SendCustomCommand);
            
            NameFeedback.LinkInputSig(trilist.StringInput[joinMap.DeviceName.JoinNumber]);
            
            NumberOfPresetsFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfPresets.JoinNumber]);

            PresetSavedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PresetSavedFeedback.JoinNumber]);
            
            CameraIsOffFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOff.JoinNumber]);
            CameraIsOffFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOn.JoinNumber]);

            IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            PanSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.PanSpeed.JoinNumber]);
            TiltSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.TiltSpeed.JoinNumber]);
            ZoomSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.ZoomSpeed.JoinNumber]);

            trilist.SetBoolSigAction(joinMap.PanLeft.JoinNumber, sig =>
            {
                if (sig) PanLeft();
                else PanStop();
            });

            trilist.SetBoolSigAction(joinMap.PanRight.JoinNumber, sig =>
            {
                if (sig) PanRight();
                else PanStop();
            });

            trilist.SetBoolSigAction(joinMap.TiltUp.JoinNumber, sig =>
            {
                if (sig) TiltUp();
                else TiltStop();
            });

            trilist.SetBoolSigAction(joinMap.TiltDown.JoinNumber, sig =>
            {
                if (sig) TiltDown();
                else TiltStop();
            });

            trilist.SetBoolSigAction(joinMap.ZoomIn.JoinNumber, sig =>
            {
                if (sig) ZoomIn();
                else ZoomStop();
            });

            trilist.SetBoolSigAction(joinMap.ZoomOut.JoinNumber, sig =>
            {
                if (sig) ZoomOut();
                else ZoomStop();
            });

            trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, CameraOn);
            trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, CameraOff);
            trilist.SetSigTrueAction(joinMap.PrivacyOn.JoinNumber, PositionPrivacy);
            trilist.SetSigTrueAction(joinMap.PrivacyOff.JoinNumber, () => RecallPreset(1));
            trilist.SetSigTrueAction(joinMap.Home.JoinNumber, PositionHome);

            trilist.SetUShortSigAction(joinMap.PanSpeed.JoinNumber, panSpeed => PanSpeed = panSpeed);
            trilist.SetUShortSigAction(joinMap.TiltSpeed.JoinNumber, tiltSpeed => TiltSpeed = tiltSpeed);
            trilist.SetUShortSigAction(joinMap.ZoomSpeed.JoinNumber, zoomSpeed => ZoomSpeed = zoomSpeed);

            trilist.SetStringSigAction(joinMap.IpAddress.JoinNumber, SetIpAddress);

            foreach (var preset in PresetNamesFeedbacks)
            {
                Debug.Console(2, "foreach: preset.Key: {0} preset.Value: {1}", preset.Key, preset.Value);
                var presetNumber = preset.Key;
                var nameJoin = joinMap.PresetNames.JoinNumber + presetNumber - 1;
                preset.Value.LinkInputSig(trilist.StringInput[nameJoin]);
                preset.Value.FireUpdate();

                var recallJoin = joinMap.PresetRecall.JoinNumber + presetNumber - 1;
                var saveJoin = joinMap.PresetSave.JoinNumber + presetNumber - 1;

                trilist.SetSigHeldAction(recallJoin, 5000, () => SavePreset((int)presetNumber), () => RecallPreset((int)presetNumber));
                trilist.SetSigTrueAction(saveJoin, () => SavePreset((int)presetNumber));
                trilist.SetStringSigAction(recallJoin, s => UpdatePresetName((int)presetNumber, s));
            }

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
                
            };
        }        

        #endregion Overrides of EssentialsBridgeableDevice

        #region ICommunicationMonitor Members

        public StatusMonitorBase CommunicationMonitor
        {
            get { return _monitor; }
        }

        #endregion

        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }
    }
}