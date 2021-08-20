using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PanasonicCameraEpi
{
    public class PanasonicCamera : CameraBase, IHasCameraPtzControl, IHasPowerControl, ICommunicationMonitor, IRoutingSource, IHasCameraFocusControl
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
        public BoolFeedback CameraIsOffFeedback { get; private set; }
        public BoolFeedback CameraIsOnFeedback { get; private set; }

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

        /// <summary>
        /// Connect feedback
        /// </summary>
        public BoolFeedback ConnectFeedback
        {
            get { return ConnectFeedback; }
            set
            {
                if (value != ConnectFeedback)
                {
                    ConnectFeedback = value;
                    ConnectFeedback.FireUpdate();
                }
            }
        }
        /// <summary>
        /// Online feedback
        /// </summary>
        public BoolFeedback OnlineFeedback
        {
            get { return OnlineFeedback; }
            set
            {
                if (value != OnlineFeedback)
                {
                    OnlineFeedback = value;
                    OnlineFeedback.FireUpdate();
                }
            }
        }
        /// <summary>
        /// Socket status feedback
        /// </summary>
        public IntFeedback SocketStatusFeedback
        {
            get { return SocketStatusFeedback; }
            set
            {
                if (value != SocketStatusFeedback)
                {
                    SocketStatusFeedback = value;
                    SocketStatusFeedback.FireUpdate();
                }
            }
        }
        /// <summary>
        /// Monitor status feedback
        /// </summary>
        public IntFeedback MonitorStatusFeedback
        {
            get { return MonitorStatusFeedback; }
            set
            {
                if (value != MonitorStatusFeedback)
                {
                    MonitorStatusFeedback = value;
                    MonitorStatusFeedback.FireUpdate();
                }
            }
        }

        public BoolFeedback PowerFeedback { get; private set; }
        /// <summary>
        /// Power property
        /// </summary>
        public bool Power
        {
            get { return Power; }
            set
            {
                if (Power == value) return;
                Power = value;
                PowerFeedback.FireUpdate();
            }
        }


        private bool _autoFocus;
        /// <summary>
        /// Auto focus feedback
        /// </summary>
        public BoolFeedback AutoFocusFeedback { get; private set; }
        /// <summary>
        /// Auto focus property
        /// </summary>
        public bool AutoFocus
        {
            get { return _autoFocus; }
            set
            {
                if (_autoFocus == value) return;
                _autoFocus = value;
                AutoFocusFeedback.FireUpdate();
            }
        }


        /// <summary>
        /// Move PTZ direction enumeration
        /// </summary>
        public enum EDirection
        {
            Stop = 0,
            Home = 1,
            PanLeft = 2,
            PanRight = 3,
            TiltUp = 4,
            TiltDown = 5,
            ZoomIn = 6,
            ZoomOut = 7,
            FocusAuto = 8,
            FocusNear = 9,
            FocusFar = 10,
            PrivacyOn = 11,
            PrivacyOff = 12
        }

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

        #region Overrides of EssentialsBridgeableDevice

        /// <summary>
        /// Link to API method replaces bridge class, this method will be called by the bridge directly
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

            // link joins to bridge
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            //ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);
            //MonitorStatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            SocketStatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            // power
            trilist.SetBoolSigAction(joinMap.PowerOn.JoinNumber, SetPower);
            PowerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn.JoinNumber]);

            trilist.SetBoolSigAction(joinMap.PowerOff.JoinNumber, SetPower);
            PowerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOff.JoinNumber]);

            // preset
            PresetCountFeedback.LinkInputSig(trilist.UShortInput[joinMap.PresetCount.JoinNumber]);
            foreach (var item in PresetNameFeedbacks)
            {
                // preset number
                var preset = (ushort)item.Key;

                // preset names
                var nameJoin = preset + joinMap.PresetNames.JoinNumber - 1;
                var nameFeedback = item.Value;
                nameFeedback.LinkInputSig(trilist.StringInput[nameJoin]);

                // preset recall
                var recallJoin = preset + joinMap.PresetRecall.JoinNumber - 1;
                trilist.SetSigHeldAction(recallJoin, PresetSaveHoldTimeMs, () => PresetSave(preset), () => RecallPreset(preset));

                // preset save/store
                var saveJoin = preset + joinMap.PresetSave.JoinNumber - 1;
                trilist.SetSigTrueAction(saveJoin, () => PresetSave(preset));
            }

            // home
            trilist.SetBoolSigAction(joinMap.Home.JoinNumber, sig => Move(sig, EDirection.Home));

            // pan
            trilist.SetBoolSigAction(joinMap.PanLeft.JoinNumber, sig => Move(sig, EDirection.PanLeft));
            trilist.SetBoolSigAction(joinMap.PanRight.JoinNumber, sig => Move(sig, EDirection.PanRight));
            trilist.SetUShortSigAction(joinMap.PanSpeed.JoinNumber, value => SetPanSpeed(value));
            PanSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.PanSpeed.JoinNumber]);

            // tilt
            trilist.SetBoolSigAction(joinMap.TiltUp.JoinNumber, sig => Move(sig, EDirection.TiltUp));
            trilist.SetBoolSigAction(joinMap.TiltDown.JoinNumber, sig => Move(sig, EDirection.TiltDown));
            trilist.SetUShortSigAction(joinMap.TiltSpeed.JoinNumber, value => SetTiltSpeed(value));
            PanSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.TiltSpeed.JoinNumber]);

            // zoom
            trilist.SetBoolSigAction(joinMap.ZoomIn.JoinNumber, sig => Move(sig, EDirection.ZoomIn));
            trilist.SetBoolSigAction(joinMap.ZoomOut.JoinNumber, sig => Move(sig, EDirection.ZoomOut));
            trilist.SetUShortSigAction(joinMap.ZoomSpeed.JoinNumber, value => SetZoomSpeed(value));
            PanSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.ZoomSpeed.JoinNumber]);

            // focus
            trilist.SetBoolSigAction(joinMap.AutoFocus.JoinNumber, sig => Move(sig, EDirection.FocusAuto));
            AutoFocusFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoFocus.JoinNumber]);

            // privacy
            trilist.SetBoolSigAction(joinMap.PrivacyOn.JoinNumber, sig => Move(sig, EDirection.PrivacyOn));
            trilist.SetBoolSigAction(joinMap.PrivacyOff.JoinNumber, sig => Move(sig, EDirection.PrivacyOff));

            // preset - analog recall & save by number
            trilist.SetUShortSigAction(joinMap.PresetRecall.JoinNumber, value =>
            {
                RecallPreset(value);
                Debug.Console(1, this, "LinkToApi PresetRecallByNumber[{0}] => RecallPreset({1})", joinMap.PresetRecall.JoinNumber, value);
            });
            trilist.SetUShortSigAction(joinMap.PresetSave.JoinNumber, value =>
            {
                PresetSave(value);
                Debug.Console(1, this, "LinkToApi PresetSaveByNumber[{0}] => SavePreset({1})", joinMap.PresetSave.JoinNumber, value);
            });

            UpdateFeedbacks();

            // online status 
            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;
                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
                UpdateFeedbacks();
            };
        }

        private void UpdateFeedbacks()
        {
            ConnectFeedback.FireUpdate();
            OnlineFeedback.FireUpdate();
            SocketStatusFeedback.FireUpdate();
            //MonitorStatusFeedback.FireUpdate();

            PowerFeedback.FireUpdate();
            PresetCountFeedback.FireUpdate();
            PanSpeedFeedback.FireUpdate();
            TiltSpeedFeedback.FireUpdate();
            ZoomSpeedFeedback.FireUpdate();

            foreach (var item in PresetNameFeedbacks)
                item.Value.FireUpdate();

            foreach (var item in PresetEnableFeedbacks)
                item.Value.FireUpdate();
        }

        #endregion


        /// <summary>
        /// Set power state
        /// </summary>
        /// <param name="state">power on/off</param>
        public void SetPower(bool state)
        {
            // Vaddio VISCA variant
            if (state)
                PowerOff();
            else
                PowerOn();
        }

        /// <summary>
        /// Move camera
        /// </summary>
        /// <param name="state">sig action true/false</param>
        /// <param name="direction">EMoveDirection direction</param>
        public void Move(bool state, EDirection direction)
        {
            // ternary >> state ? [moving] : [stop]
            switch (direction)
            {
                case EDirection.Home:
                    {
                        // Vaddio VISCA variant
                        if (state)
                        {
                            PositionHome();
                        }
                        break;
                    }
                case EDirection.PanLeft:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            PanLeft();
                        }
                        else
                        {
                            PanStop();
                        }
                        break;
                    }
                case EDirection.PanRight:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            PanRight();
                        }
                        else
                        {
                            PanStop();
                        }
                        break;
                    }
                case EDirection.TiltUp:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            TiltUp();
                        }
                        else
                        {
                            TiltStop();
                        }
                        break;
                    }
                case EDirection.TiltDown:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            TiltDown();
                        }
                        else
                        {
                            TiltStop();
                        }
                        break;
                    }
                case EDirection.ZoomIn:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            ZoomIn();
                        }
                        else
                        {
                            ZoomStop();
                        }
                        break;
                       }
                case EDirection.ZoomOut:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            ZoomOut();
                        }
                        else
                        {
                            ZoomStop();
                        }
                        break;
                    }
                case EDirection.FocusAuto:
                    {
                        
                        break;
                    }
                case EDirection.FocusNear:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            FocusNear();
                        }
                        else
                        {
                            FocusStop();
                        }
                        break;
                    }
                case EDirection.FocusFar:
                    {
                        // Vaddio VISCA variant
                        if (state == true)
                        {
                            FocusFar();
                        }
                        else
                        {
                            FocusStop();
                        }
                        break;
                    }
                //case EDirection.PrivacyOn:
                //    {
                //        if (cameraConfig.PrivacyOnPreset == 0) return;
                //        RecallPreset(cameraConfig.PrivacyOnPreset);
                //        break;
                //    }
                //case EDirection.PrivacyOff:
                //    {
                //        if (cameraConfig.PrivacyOffPreset == 0) return;
                //        RecallPreset(cameraConfig.PrivacyOffPreset);
                //        break;
                //    }
            }
        }
        /// <summary>
        /// Sets pan speed, range 0-18 (hex)
        /// </summary>
        /// <param name="value"></param>
        public void SetPanSpeed(uint value)
        {
            if (value == 0 || value > 30) return;
            PanSpeed = (int)value;
        }
        /// <summary>
        /// Sets tilt speed, range 0-18 (hex)
        /// </summary>
        /// <param name="value"></param>
        public void SetTiltSpeed(uint value)
        {
            if (value == 0 || value > 30) return;
            TiltSpeed = (int)value;
        }

        /// <summary>
        /// Sets zoom speed, range 0-7 (hex)
        /// </summary>
        /// <param name="value"></param>
        public void SetZoomSpeed(uint value)
        {
            if (value == 0 || value > 99) return;
            ZoomSpeed = (int)value;
        }

        /// <summary>
        /// Sets focus speed, range 0-7 (hex)
        /// </summary>
        /// <param name="value"></param>
        public void SetFocusSpeed(uint value)
        {
            if (value == 0 || value > 99) return;
            TiltSpeed = (int)value;
        }

        /// <summary>
        /// Recall preset
        /// </summary>
        /// <param name="value">preset 1...max</param>
        public void RecallPreset(uint value)
        {
            if (value <= 0 || value > PresetMax) return;

            // Vaddio VISCA variant
            RecallPreset((int)value);
        }

        private const int PresetSaveHoldTimeMs = 5000; // 5s
        private const int PresetMax = 16;
        private uint _presetCount;
        /// <summary>
        /// Preset count feedback
        /// </summary>
        public IntFeedback PresetCountFeedback { get; private set; }
        /// <summary>
        /// Preset count property
        /// </summary>
        public uint PresetCount
        {
            get { return (uint)_presetCount; }
            set
            {
                if (_presetCount == value) return;
                _presetCount = value;
                PresetCountFeedback.FireUpdate();
            }
        }

        /// <summary>
        /// Preset name feedbacks
        /// </summary>
        public Dictionary<uint, StringFeedback> PresetNameFeedbacks { get; private set; }
        /// <summary>
        /// Preset enable feedbacks
        /// </summary>
        public Dictionary<uint, BoolFeedback> PresetEnableFeedbacks { get; private set; }


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

        public void PresetSave(int preset)
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

        //public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
        //{
        //    this.LinkToApiExt(trilist, joinStart, joinMapKey);
        //}

        #endregion

        #region ICommunicationMonitor Members

        public StatusMonitorBase CommunicationMonitor
        {
            get { return _monitor; }
        }

        #endregion

        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

        #region IHasPowerControl Members

        public void PowerOff()
        {
            _queue.EnqueueCmd(_cmd.PowerOffCommand);
        }

        public void PowerOn()
        {
            _queue.EnqueueCmd(_cmd.PowerOnCommand);
        }

        public void PowerToggle()
        {
            if (IsPoweredOn)
                PowerOff();
            else
                PowerOn();
        }

        #endregion

        #region IHasCameraFocusControl Members

        public void FocusFar()
        {
            throw new NotImplementedException();
        }

        public void FocusNear()
        {
            throw new NotImplementedException();
        }

        public void FocusStop()
        {
            throw new NotImplementedException();
        }

        public void TriggerAutoFocus()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}