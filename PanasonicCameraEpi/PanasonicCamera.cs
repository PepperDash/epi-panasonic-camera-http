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
        private readonly PanasonicCmdBuilder _cmd;
        private readonly PanasonicResponseHandler _responseHandler;
        private readonly CommandQueue _queue;

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
                throw new NullReferenceException("No valid control method found");
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
                    Debug.Console(0, Debug.ErrorLogLevel.Warning, "Hostname or address not found");
                    throw;
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
 
            var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);

			_responseHandler = new PanasonicResponseHandler();

            var monitorConfig = cameraConfig.CommunicationMonitor ??
                                new CommunicationMonitorConfig
                                {
                                    PollInterval = 60000,
                                    TimeToWarning = 180000,
                                    TimeToError = 300000,
                                    PollString = "cgi-bin/aw_ptz?cmd=%23O&res=1"
                                };

            var tempClient = comms as GenericHttpClient;
            if(tempClient == null) 
            {
                _monitor = new GenericCommunicationMonitor(this, tempClient, monitorConfig);
                tempClient.TextReceived += _responseHandler.HandleResponseReceeved;
                throw new NotImplementedException("Need to create a command queue for serial");
			}
			else
            {
                _monitor = new PanasonicHttpCameraMonitor(this, tempClient, monitorConfig);
                var queue = new HttpCommandQueue(comms);
                queue.ResponseReceived += _responseHandler.HandleResponseReceived;
                _queue = queue;
            }

            _cmd = new PanasonicCmdBuilder(12, 25, 12);

            Presets = cameraConfig.Presets.OrderBy(x => x.Id);
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

            NumberOfPresetsFeedback = new IntFeedback(() => Presets.Count());
            NumberOfPresetsFeedback.FireUpdate();

            PanSpeedFeedback = new IntFeedback(() => PanSpeed);
            TiltSpeedFeedback = new IntFeedback(() => TiltSpeed);
            ZoomSpeedFeedback = new IntFeedback(() => ZoomSpeed);

            PanSpeedFeedback.FireUpdate();
            TiltSpeedFeedback.FireUpdate();
            ZoomSpeedFeedback.FireUpdate();

            PresetNamesFeedbacks = Presets.ToDictionary(x => (uint)x.Id, x => new StringFeedback(() => x.Name));

            foreach (var feedback in PresetNamesFeedbacks)
            {
                feedback.Value.FireUpdate();
            }

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