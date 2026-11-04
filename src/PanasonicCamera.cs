using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Core;
using Crestron.SimplSharp;
using PepperDash.Core.Logging;


namespace PepperDash.Essentials.Plugins
{
    public class PanasonicCamera : CameraBase, IBridgeAdvanced, IHasCameraPtzControl, IHasCameraPresets, IHasCameraOff, IHasCameraPrivacy, IHasPowerControlWithFeedback, ICommunicationMonitor, IRoutingSource, IDisposable
    {
        private readonly StatusMonitorBase _monitor;
        private readonly PanasonicCmdBuilder _cmd;
        private readonly PanasonicResponseHandler _responseHandler;
        private readonly HttpCommandQueue _queue;
        private readonly Dictionary<uint, PanasonicCameraPreset> _presets;

        // Replaced (never mutated) on change so readers such as the mobile-control messenger always see a consistent list
        private volatile List<CameraPreset> _presetList = new List<CameraPreset>();
        public List<CameraPreset> Presets { get { return _presetList; } }

        public event EventHandler<EventArgs> PresetsListHasChanged;
        private const int PresetSavedDurationMs = 5000;
        private readonly Timer _presetSavedTimer;

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

        public PanasonicCamera(DeviceConfig config)
            : base(config)
        {
            // OutputPorts is created by CameraBase
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom;
            _presetSavedTimer = new Timer(_ => PresetSavedBool = false, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

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

            // Check if it's HTTP communication - assume hostname from config
            var hostname = cameraConfig.Control?.TcpSshProperties?.Address;
            if (string.IsNullOrEmpty(hostname))
            {
                throw new NotImplementedException("Hostname is empty");
			}
            _monitor = new PanasonicHttpCameraMonitor(this, hostname, cameraConfig.CommunicationMonitor);
            var pacing = cameraConfig.Pacing > 0 ? cameraConfig.Pacing : HttpCommandQueue.DefaultPacingMs;
            _queue = new HttpCommandQueue(hostname, this.Key, pacing);
            _queue.ResponseReceived += _responseHandler.HandleHttpResponse;

            _cmd = new PanasonicCmdBuilder(12, 25, 12, cameraConfig.HomeCommand, cameraConfig.PrivacyCommand);
            _presets = cameraConfig.Presets.ToDictionary(x => (uint)x.Id);

            OutputPorts.Add(new RoutingOutputPort(
				RoutingPortNames.AnyVideoOut, eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi,
				null,
				this
			));

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
        protected override bool CustomActivate()
        {
            SetupFeedbacks();
            _responseHandler.CameraPoweredOn += (sender, args) =>
                {
                    IsPoweredOn = true;
                    CameraIsOffFeedback.FireUpdate();
                    PowerIsOnFeedback.FireUpdate();
                };

            _responseHandler.CameraPoweredOff += (sender, args) =>
                {
                    IsPoweredOn = false;
                    CameraIsOffFeedback.FireUpdate();
                    PowerIsOnFeedback.FireUpdate();
                };

            _monitor.StatusChange += HandleMonitorStatusChange;
            _monitor.Start();

            // EssentialsDevice.CustomActivate creates the Mobile Control messengers for the interfaces this
            // device implements. It must run after SetupFeedbacks so the feedbacks the messengers subscribe
            // to (PowerIsOnFeedback, ...) exist.
            return base.CustomActivate();
        }        

        private void HandleMonitorStatusChange(object sender, MonitorStatusChangeEventArgs e)
        {
            this.LogInformation("Status: {status}", e.Message);
        }

        private void SetupFeedbacks()
        {
            NameFeedback = new StringFeedback("NameFeedback", () => Name);
            NameFeedback.FireUpdate();

            for (uint x = 1; x <= 10; x++)
            {
                var index = x;
                if (_presets.ContainsKey(index))
                    continue;

                _presets.Add(index, new PanasonicCameraPreset {Id = (int) index, Name = String.Empty});
            }

            NumberOfPresetsFeedback = new IntFeedback("NumberOfPresetsFeedback", () => _presets.Values.Count(x => !String.IsNullOrEmpty(x.Name)));
            NumberOfPresetsFeedback.FireUpdate();

            PanSpeedFeedback = new IntFeedback("PanSpeedFeedback", () => PanSpeed);
            TiltSpeedFeedback = new IntFeedback("TiltSpeedFeedback", () => TiltSpeed);
            ZoomSpeedFeedback = new IntFeedback("ZoomSpeedFeedback", () => ZoomSpeed);
            PresetSavedFeedback = new BoolFeedback("PresetSavedFeedback", () => PresetSavedBool); 

            PanSpeedFeedback.FireUpdate();
            TiltSpeedFeedback.FireUpdate();
            ZoomSpeedFeedback.FireUpdate();

            PresetNamesFeedbacks = _presets.ToDictionary(x => x.Key, x => new StringFeedback($"PresetName{x.Key}", () => x.Value.Name));

            foreach (var feedback in PresetNamesFeedbacks)
                feedback.Value.FireUpdate();

            CameraIsOffFeedback = new BoolFeedback("CameraIsOffFeedback", () => !IsPoweredOn);
            CameraIsOffFeedback.FireUpdate(); 

            PowerIsOnFeedback = new BoolFeedback("PowerIsOnFeedback", () => IsPoweredOn);
            PowerIsOnFeedback.FireUpdate();

            RebuildPresetList();
        }

        private void RebuildPresetList()
        {
            _presetList = _presets.Values
                .OrderBy(p => p.Id)
                .Select(p => new CameraPreset(p.Id, p.Name ?? string.Empty, !string.IsNullOrEmpty(p.Name), true))
                .ToList();
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
        public BoolFeedback PowerIsOnFeedback { get; private set; }

        public void CameraOff()
        {
            _queue.EnqueueCmd(_cmd.PowerOffCommand);
        }

        #endregion

        #region IHasPowerControlWithFeedback Members

        public void PowerOn()
        {
            _queue.EnqueueCmd(_cmd.PowerOnCommand);
        }

        public void PowerOff()
        {
            CameraOff();
        }

        public void PowerToggle()
        {
            if (IsPoweredOn)
                PowerOff();
            else
                PowerOn();
        }

        #endregion

        #region IHasCameraPrivacy Members

        public void PrivacyOn()
        {
            _queue.EnqueueCmd(_cmd.PrivacyCommand);
        }

        public void PrivacyOff()
        {
            // preset 1 is the "return" position for privacy
            PresetSelect(1);
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

        public void PresetSelect(int preset)
        {
            if (!IsPoweredOn)
                _queue.EnqueueCmd(_cmd.PowerOnCommand);

            _queue.EnqueueCmd(_cmd.PresetRecallCommand(preset));	        
        }

        public void PresetStore(int preset, string description)
        {
            _queue.EnqueueCmd(_cmd.PresetSaveCommand(preset));
            PresetSavedBool = true;
            
            // one-shot: restarts the 5 second window each time a preset is stored
            _presetSavedTimer.Change(PresetSavedDurationMs, System.Threading.Timeout.Infinite);

            if (!string.IsNullOrEmpty(description))
                UpdatePresetName(preset, description);
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
			    this.LogInformation("Changing IP address: {address}", address);

			    Config.Properties["control"]["tcpSshProperties"]["address"] = address;
			    this.LogDebug("Properties: {properties}", Config.Properties.ToString());
			    SetConfig(Config);
			    // Update hostname in config - the monitor will use the new address
			    // Note: With HttpClient approach, the monitor handles the hostname directly
			}
			catch (Exception e)
			{
				this.LogError(e, "Error setting IP address: {message}", e.Message);
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

            RebuildPresetList();
            PresetsListHasChanged?.Invoke(this, EventArgs.Empty);

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

            this.LogInformation("Linking to Trilist {trilistId}", trilist.ID.ToString("X"));
            this.LogInformation("Linking to Bridge Type {bridgeType}", GetType().Name);

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

            trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, PowerOn);
            trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, PowerOff);
            trilist.SetSigTrueAction(joinMap.PrivacyOn.JoinNumber, PrivacyOn);
            trilist.SetSigTrueAction(joinMap.PrivacyOff.JoinNumber, PrivacyOff);
            trilist.SetSigTrueAction(joinMap.Home.JoinNumber, PositionHome);

            trilist.SetUShortSigAction(joinMap.PanSpeed.JoinNumber, panSpeed => PanSpeed = panSpeed);
            trilist.SetUShortSigAction(joinMap.TiltSpeed.JoinNumber, tiltSpeed => TiltSpeed = tiltSpeed);
            trilist.SetUShortSigAction(joinMap.ZoomSpeed.JoinNumber, zoomSpeed => ZoomSpeed = zoomSpeed);

            trilist.SetStringSigAction(joinMap.IpAddress.JoinNumber, SetIpAddress);

            foreach (var preset in PresetNamesFeedbacks)
            {
                this.LogDebug("Preset {presetKey}: {presetValue}", preset.Key, preset.Value);
                var presetNumber = preset.Key;
                var nameJoin = joinMap.PresetNames.JoinNumber + presetNumber - 1;
                preset.Value.LinkInputSig(trilist.StringInput[nameJoin]);
                preset.Value.FireUpdate();

                var recallJoin = joinMap.PresetRecall.JoinNumber + presetNumber - 1;
                var saveJoin = joinMap.PresetSave.JoinNumber + presetNumber - 1;

                trilist.SetSigHeldAction(recallJoin, 5000, () => PresetStore((int)presetNumber, null), () => PresetSelect((int)presetNumber));
                trilist.SetSigTrueAction(saveJoin, () => PresetStore((int)presetNumber, null));
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

        public void Dispose()
        {
            _presetSavedTimer?.Dispose();
        }
    }
}