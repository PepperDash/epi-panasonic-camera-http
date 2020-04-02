using System;
using System.Collections.Generic;
using System.Linq;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicCamera : CameraBase, IHasCameraPtzControl, IHasCameraOff, ICommunicationMonitor, IBridge
    {
        private readonly StatusMonitorBase _monitor;
		private readonly IBasicCommunication _client;
        private readonly PanasonicCmdBuilder _cmd;
        private readonly PanasonicResponseHandler _responseHandler;

        public bool IsPoweredOn { get; private set; }
        public IEnumerable<PanasonicCameraPreset> Presets { get; private set; }
        public Dictionary<uint, StringFeedback> PresetNamesFeedbacks { get; private set; }
        public IntFeedback NumberOfPresetsFeedback { get; private set; }
        public StringFeedback NameFeedback { get; private set; }
        //public StringFeedback ComsFeedback { get; set; }
        public BoolFeedback IsOnlineFeedback { get { return _monitor.IsOnlineFeedback; } }		
        public IntFeedback PanSpeedFeedback { get; private set; }
        public IntFeedback ZoomSpeedFeedback { get; private set; }
        public IntFeedback TiltSpeedFeedback { get; private set; }

        public static void LoadPlugin()
        {
            DeviceFactory.AddFactoryForType("panasonicHttpCamera", BuildDevice);
        }

        public static PanasonicCamera BuildDevice(DeviceConfig config)
        {
            var method = config.Properties["control"].Value<string>("method");

            if (method == null)
            {
                Debug.Console(0, Debug.ErrorLogLevel.Warning, "No valid control method found");
                return null;
            }

            IBasicCommunication comms;

            if (method.ToLower() == "http")
            {
                try
                {
                    comms = new GenericHttpClient(string.Format("{0}-httpClient", config.Key), config.Name,
                        config.Properties["control"]["tcpSshProperties"].Value<string>("address"));

                    DeviceManager.AddDevice(comms);
                }
                catch (NullReferenceException)
                {
                    Debug.LogError(Debug.ErrorLogLevel.Error, String.Format("Hostname or address not found"));
                    return null;
                }
            }
            else
            {
                comms = CommFactory.CreateCommForDevice(config);
            }

            return new PanasonicCamera(comms, config);
        }

        public PanasonicCamera(IBasicCommunication comms, DeviceConfig config)
            : base(config.Key, config.Name)
        {
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom | eCameraCapabilities.Focus;

            _client = comms;
 
            var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);

			_responseHandler = new PanasonicResponseHandler();

            var tempClient = _client as GenericHttpClient;

            if(tempClient == null) {
                _client.TextReceived += _responseHandler.HandleResponseRecived;
			}
			else
            {
                tempClient.ResponseRecived += _responseHandler.HandleResponseRecived;
            }

            var monitorConfig = cameraConfig.CommunicationMonitor ??
                                new CommunicationMonitorConfig
                                {
                                    PollInterval = 60000,
                                    TimeToWarning = 180000,
                                    TimeToError = 300000,
                                    PollString = "O"
                                };

            _monitor = new GenericCommunicationMonitor(this, _client, monitorConfig);

            _cmd = new PanasonicCmdBuilder(25, 25, 25);
            

            Presets = cameraConfig.Presets.OrderBy(x => x.Id);
            PresetNamesFeedbacks = new Dictionary<uint, StringFeedback>();
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

            _monitor.Start();

            return true;
        }

        private void SetupFeedbacks()
        {
            NameFeedback = new StringFeedback(() => Name);
            NameFeedback.FireUpdate();

            NumberOfPresetsFeedback = new IntFeedback(() => Presets.Count());
            NumberOfPresetsFeedback.FireUpdate();

            PanSpeedFeedback = new IntFeedback(() => PanSpeed);
            TiltSpeedFeedback = new IntFeedback(() => TiltSpeed);
            ZoomSpeedFeedback = new IntFeedback(() => ZoomSpeed);

            PanSpeedFeedback.FireUpdate();
            TiltSpeedFeedback.FireUpdate();
            ZoomSpeedFeedback.FireUpdate();

            Presets.ToList().ForEach(preset =>
                {
                    PresetNamesFeedbacks.Add((uint)preset.Id, new StringFeedback(() => preset.Name));
                    PresetNamesFeedbacks[(uint)preset.Id].FireUpdate();
                });

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
                _cmd.ZoomSpeed = (ZoomSpeed <= 0 || ZoomSpeed >= 50 ? 25 : value);
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
            _client.SendText(_cmd.HomeCommand);
        }

        public void PositionPrivacy()
        {
            _client.SendText(_cmd.PrivacyCommand);
        }

        #endregion

        #region IHasCameraPanControl Members

        public void PanLeft()
        {
            _client.SendText(_cmd.PanLeftCommand);
        }

        public void PanRight()
        {
            _client.SendText(_cmd.PanRightCommand);
        }

        public void PanStop()
        {
            _client.SendText(_cmd.PanStopCommand);
        }

        #endregion

        #region IHasCameraTiltControl Members

        public void TiltDown()
        {
	        _client.SendText(_cmd.TiltDownCommand);			
        }

        public void TiltUp()
        {
            _client.SendText(_cmd.TiltUpCommand);
        }

        public void TiltStop()
        {
            _client.SendText(_cmd.TiltStopCommand);
        }    

        #endregion

        #region IHasCameraOff Members
        public BoolFeedback CameraIsOffFeedback { get; private set; }

        public void CameraOn()
        {
            _client.SendText(_cmd.PowerOnCommand);
        }

        public void CameraOff()
        {
            _client.SendText(_cmd.PowerOffCommand);
        }

        #endregion

        #region IHasCameraZoomControl Members

        public void ZoomIn()
        {
            _client.SendText(_cmd.ZoomInCommand);
        }

        public void ZoomOut()
        {
            _client.SendText(_cmd.ZoomOutCommand);
        }

        public void ZoomStop()
        {
            _client.SendText(_cmd.ZoomStopCommand);
        }

        #endregion

        public void SendCustomCommand(string cmd)
        {
            _client.SendText(PanasonicCmdBuilder.BuildCustomCommand(cmd));
        }

        public void RecallPreset(int preset)
        {
			// TODO: Remove debug statement after working through camera preset issues noted in PanasonicCameraBridge.cs
	        Debug.Console(2, this, "RecallPreset({0})", preset);
	        try
	        {
		        _client.SendText(_cmd.PresetRecallCommand(preset));
	        }
	        catch (Exception e)
	        {
		        Debug.Console(2, this, "Recall Preset {0} Exception: {1}", preset, e);
	        }
        }

        public void SavePreset(int preset)
        {
            _client.SendText(_cmd.PresetSaveCommand(preset));
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
    }
}