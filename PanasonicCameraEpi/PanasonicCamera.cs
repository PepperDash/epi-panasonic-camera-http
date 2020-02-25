using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PanasonicCameraEpi
{
    public class PanasonicCamera : CameraBase, IHasCameraPtzControl, IHasCameraOff, ICommunicationMonitor, IBridge
    {
        readonly StatusMonitorBase monitor;
        readonly GenericHttpClient client;
        readonly PanasonicCmdBuilder cmd;
        readonly PanasonicResponseHandler responseHandler;

        public bool IsPoweredOn { get; private set; }
        public IEnumerable<PanasonicCameraPreset> Presets { get; private set; }
        public Dictionary<uint, StringFeedback> PresetNamesFeedbacks { get; private set; }
        public IntFeedback NumberOfPresetsFeedback { get; private set; }
        public StringFeedback NameFeedback { get; private set; }
        public StringFeedback ComsFeedback { get; private set; }
        public BoolFeedback IsOnlineFeedback { get { return monitor.IsOnlineFeedback; } }

        public PanasonicCamera(DeviceConfig config)
            : base(config.Key, config.Name)
        {
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom | eCameraCapabilities.Focus; 
            var cameraConfig = PanasonicCameraPropsConfig.FromDeviceConfig(config);

            client = new GenericHttpClient(string.Format("{0}-client", config.Key), "", cameraConfig.Control.TcpSshProperties.Address);

            var monitorConfig = (cameraConfig.CommunicationMonitor != null) ? cameraConfig.CommunicationMonitor : 
                new CommunicationMonitorConfig() { PollInterval = 10000, TimeToWarning = 60000, TimeToError = 120000, PollString = "O" };

            monitor = new GenericCommunicationMonitor(this, client, monitorConfig);

            cmd = new PanasonicCmdBuilder(25, 25, 25);
            responseHandler = new PanasonicResponseHandler(client);

            Presets = cameraConfig.Presets.OrderBy(x => x.Id);
            PresetNamesFeedbacks = new Dictionary<uint, StringFeedback>();
        }

        public override bool CustomActivate()
        {
            SetupFeedbacks();
            responseHandler.CameraPoweredOn += (sender, args) =>
                {
                    IsPoweredOn = true;
                    CameraIsOffFeedback.FireUpdate();
                };

            responseHandler.CameraPoweredOff += (sender, args) =>
                {
                    IsPoweredOn = false;
                    CameraIsOffFeedback.FireUpdate();
                };

            monitor.Start();

            return true;
        }

        private void SetupFeedbacks()
        {
            NameFeedback = new StringFeedback(() => Name);
            NameFeedback.FireUpdate();

            NumberOfPresetsFeedback = new IntFeedback(() => Presets.Count());
            NumberOfPresetsFeedback.FireUpdate();

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
            get { return cmd.PanSpeed; }
            set { cmd.PanSpeed = value; }
        }

        public int ZoomSpeed 
        {
            get { return cmd.ZoomSpeed; }
            set { cmd.ZoomSpeed = value; }
        }

        public int TiltSpeed
        {
            get { return cmd.TiltSpeed; }
            set { cmd.TiltSpeed = value; }
        }

        #region IHasCameraPtzControl Members

        public void PositionHome()
        {
            client.SendText(cmd.HomeCommand);
        }

        public void PositionPrivacy()
        {
            client.SendText(cmd.PrivacyCommand);
        }

        #endregion

        #region IHasCameraPanControl Members

        public void PanLeft()
        {
            client.SendText(cmd.PanLeftCommand);
        }

        public void PanRight()
        {
            client.SendText(cmd.PanRightCommand);
        }

        public void PanStop()
        {
            client.SendText(cmd.PanStopCommand);
        }

        #endregion

        #region IHasCameraTiltControl Members

        public void TiltDown()
        {
            client.SendText(cmd.TiltUpCommand);
        }

        public void TiltUp()
        {
            client.SendText(cmd.TiltUpCommand);
        }

        public void TiltStop()
        {
            client.SendText(cmd.TiltStopCommand);
        }    

        #endregion

        #region IHasCameraOff Members
        public BoolFeedback CameraIsOffFeedback { get; private set; }

        public void CameraOn()
        {
            client.SendText(cmd.PowerOnCommand);
        }

        public void CameraOff()
        {
            client.SendText(cmd.PowerOffCommand);
        }

        #endregion

        #region IHasCameraZoomControl Members

        public void ZoomIn()
        {
            client.SendText(cmd.ZoomInCommand);
        }

        public void ZoomOut()
        {
            client.SendText(cmd.ZoomOutCommand);
        }

        public void ZoomStop()
        {
            client.SendText(cmd.ZoomStopCommand);
        }

        #endregion

        public void SendCustomCommand(string cmd)
        {
            client.SendText(PanasonicCmdBuilder.BuildCustomCommand(cmd));
        }

        public void RecallPreset(int preset)
        {
            client.SendText(cmd.PresetRecallCommand(preset));
        }

        public void SavePreset(int preset)
        {
            client.SendText(cmd.PresetSaveCommand(preset));
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
            get { return monitor; }
        }

        #endregion
    }
}