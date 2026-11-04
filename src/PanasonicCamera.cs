using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Core;
using Crestron.SimplSharp;
using PepperDash.Core.Logging;


namespace PepperDash.Essentials.Plugins
{
    public class PanasonicCamera : CameraBase, IBridgeAdvanced, IHasCameraPtzControl, IHasCameraFocusControl, IHasAutoFocusMode, IHasCameraPresets, IHasCameraOff, IHasCameraPrivacy, IHasPowerControlWithFeedback, ICommunicationMonitor, IRoutingSource, IDeviceInfoProvider, IDisposable
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
        public IntFeedback FocusSpeedFeedback { get; private set; }
        public BoolFeedback AutoFocusIsOnFeedback { get; private set; }

        // last auto focus state reported by the camera (reply to #D1 / #D1[0|1]); false until the camera reports
        private volatile bool _autoFocusOn;
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
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom | eCameraCapabilities.Focus;
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
            var monitor = new PanasonicHttpCameraMonitor(this, hostname, cameraConfig.CommunicationMonitor);
            // poll replies ("p1"/"p0") are how the camera's power state is learned at startup
            monitor.ResponseReceived += _responseHandler.HandleHttpResponse;
            _monitor = monitor;

            DeviceInfo = new DeviceInfo();
            SetNetworkInfo(hostname);
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

            AddPostActivationAction(() =>
            {
                if (cameraConfig.FocusSpeed == 0)
                    return;

                FocusSpeed = cameraConfig.FocusSpeed;
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
                    RequestDeviceInfoIfMissing();
                };

            _responseHandler.CameraPoweredOff += (sender, args) =>
                {
                    IsPoweredOn = false;
                    CameraIsOffFeedback.FireUpdate();
                    PowerIsOnFeedback.FireUpdate();
                    RequestDeviceInfoIfMissing();
                };

            _responseHandler.ResponseDeviceInfo += HandleDeviceInfoResponse;
            _responseHandler.AutoFocusChanged += (sender, isOn) =>
            {
                _autoFocusOn = isOn;
                AutoFocusIsOnFeedback.FireUpdate();
            };

            _monitor.StatusChange += HandleMonitorStatusChange;
            _monitor.Start();

            // learn the camera's auto focus state; the reply ("d10"/"d11") updates AutoFocusIsOnFeedback
            _queue.EnqueueCmd(_cmd.AutoFocusQueryCommand);

            // EssentialsDevice.CustomActivate creates the Mobile Control messengers for the interfaces this
            // device implements. It must run after SetupFeedbacks so the feedbacks the messengers subscribe
            // to (PowerIsOnFeedback, ...) exist.
            return base.CustomActivate();
        }        

        /// <summary>
        /// Registers the core messengers for every interface this camera implements, except the camera
        /// control messenger, which is replaced by <see cref="PanasonicCameraControlMessenger"/> (core's plus privacy).
        /// </summary>
        protected override void CreateMobileControlMessengers()
        {
            var controller = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();
            if (controller == null)
            {
                this.LogWarning("No IMobileControl controller found; Mobile Control messengers will not be registered");
                return;
            }

            // The registry creates a messenger per interface in this list; leaving out IHasCameraControls
            // skips core's CameraControlMessenger so ours does not double-handle every action.
            var interfaces = GetType().GetInterfaces().Where(i => i != typeof(IHasCameraControls));
            controller.AddDefaultMessengersForDevice(this, interfaces);

            controller.AddDeviceMessenger(
                new PanasonicCameraControlMessenger($"{Key}-hasCamerasWithControls-{controller.Key}", this, $"/device/{Key}"));
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
            FocusSpeedFeedback = new IntFeedback("FocusSpeedFeedback", () => FocusSpeed);
            AutoFocusIsOnFeedback = new BoolFeedback("AutoFocusIsOnFeedback", () => _autoFocusOn);
            PresetSavedFeedback = new BoolFeedback("PresetSavedFeedback", () => PresetSavedBool); 

            PanSpeedFeedback.FireUpdate();
            TiltSpeedFeedback.FireUpdate();
            ZoomSpeedFeedback.FireUpdate();
            FocusSpeedFeedback.FireUpdate();
            AutoFocusIsOnFeedback.FireUpdate();

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

        public int FocusSpeed
        {
            get { return _cmd.FocusSpeed; }
            set
            {
                _cmd.FocusSpeed = value;
                FocusSpeedFeedback.FireUpdate();
            }
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

        #region IHasCameraFocusControl Members

        public void FocusNear()
        {
            _queue.EnqueueCmd(_cmd.FocusNearCommand);
        }

        public void FocusFar()
        {
            _queue.EnqueueCmd(_cmd.FocusFarCommand);
        }

        public void FocusStop()
        {
            _queue.EnqueueCmd(_cmd.FocusStopCommand);
        }

        /// <summary>
        /// One-touch auto focus (aw_cam OSE:69:1).
        /// </summary>
        public void TriggerAutoFocus()
        {
            _queue.EnqueueCmd(_cmd.OneTouchAutoFocusCommand);
        }

        #endregion

        #region IHasAutoFocusMode Members

        // The camera ignores manual focus speed commands while auto focus is on (ER3), so the mode matters to callers.
        public void SetFocusModeAuto()
        {
            _queue.EnqueueCmd(_cmd.AutoFocusOnCommand);
        }

        public void SetFocusModeManual()
        {
            _queue.EnqueueCmd(_cmd.AutoFocusOffCommand);
        }

        public void ToggleFocusMode()
        {
            if (_autoFocusOn)
                SetFocusModeManual();
            else
                SetFocusModeAuto();
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
            FocusSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.FocusSpeed.JoinNumber]);
            AutoFocusIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoFocusOn.JoinNumber]);
            AutoFocusIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoFocusOff.JoinNumber]);

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
            trilist.SetUShortSigAction(joinMap.FocusSpeed.JoinNumber, focusSpeed => FocusSpeed = focusSpeed);

            trilist.SetBoolSigAction(joinMap.FocusNear.JoinNumber, sig =>
            {
                if (sig) FocusNear();
                else FocusStop();
            });

            trilist.SetBoolSigAction(joinMap.FocusFar.JoinNumber, sig =>
            {
                if (sig) FocusFar();
                else FocusStop();
            });

            trilist.SetSigTrueAction(joinMap.AutoFocusTrigger.JoinNumber, TriggerAutoFocus);
            trilist.SetSigTrueAction(joinMap.AutoFocusOn.JoinNumber, SetFocusModeAuto);
            trilist.SetSigTrueAction(joinMap.AutoFocusOff.JoinNumber, SetFocusModeManual);

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

        #region IDeviceInfoProvider Members

        public DeviceInfo DeviceInfo { get; private set; }

        public event DeviceInfoChangeHandler DeviceInfoChanged;

        public void UpdateDeviceInfo()
        {
            _queue.EnqueueCmd(PanasonicCmdBuilder.DeviceInfoCommand);
        }

        private void RequestDeviceInfoIfMissing()
        {
            if (string.IsNullOrEmpty(DeviceInfo.FirmwareVersion))
                UpdateDeviceInfo();
        }

        private void HandleDeviceInfoResponse(object sender, ResponseDeviceInfoEventArgs args)
        {
            DeviceInfo.MacAddress = args.MacAddress;
            DeviceInfo.SerialNumber = args.SerialNumber;
            DeviceInfo.FirmwareVersion = args.FirmwareVersion;
            DeviceInfoChanged?.Invoke(this, new DeviceInfoEventArgs(DeviceInfo));
        }

        private void SetNetworkInfo(string address)
        {
            DeviceInfo.HostName = address;

            System.Net.IPAddress ip;
            if (System.Net.IPAddress.TryParse(address, out ip))
            {
                DeviceInfo.IpAddress = ip.ToString();
                return;
            }

            // hostname in config: resolve off the calling thread, never block activation on DNS
            Task.Run(() =>
            {
                try
                {
                    var resolved = System.Net.Dns.GetHostAddresses(address).FirstOrDefault();
                    if (resolved == null)
                        return;

                    DeviceInfo.IpAddress = resolved.ToString();
                    DeviceInfoChanged?.Invoke(this, new DeviceInfoEventArgs(DeviceInfo));
                }
                catch (Exception ex)
                {
                    this.LogWarning("Unable to resolve {hostname}: {message}", address, ex.Message);
                }
            });
        }

        #endregion

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