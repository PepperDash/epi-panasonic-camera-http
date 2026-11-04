using System;
using System.Text;
using PepperDash.Core;
using Serilog;

namespace PepperDash.Essentials.Plugins
{
    public class PanasonicCmdBuilder
    {
        private const string CmdHeader = "cgi-bin/aw_ptz?cmd=%23";
        private const string CmdSuffix = "&res=1";

        // One-touch AF is an aw_cam command (spec 3.2.1.1), not aw_ptz
        private const string OneTouchAutoFocus = "cgi-bin/aw_cam?cmd=OSE:69:1&res=1";

        public string PanStopCommand { get; private set; }
        public string TiltStopCommand { get; private set; }
        public string ZoomStopCommand { get; private set; }
        public string PowerOnCommand { get; private set; }
        public string PowerOffCommand { get; private set; }
        public string HomeCommand { get; private set; }
        public string PrivacyCommand { get; private set; }

        public string PanLeftCommand { get; private set; }
        public string PanRightCommand { get; private set; }
        public string ZoomInCommand { get; private set; }
        public string ZoomOutCommand { get; private set; }
        public string TiltUpCommand { get; private set; }
        public string TiltDownCommand { get; private set; }

        public string FocusStopCommand { get; private set; }
        public string FocusNearCommand { get; private set; }
        public string FocusFarCommand { get; private set; }
        public string AutoFocusOnCommand { get; private set; }
        public string AutoFocusOffCommand { get; private set; }
        public string AutoFocusQueryCommand { get; private set; }
        public string OneTouchAutoFocusCommand { get; private set; }

        public PanasonicCmdBuilder(int panSpeed, int zoomSpeed, int tiltSpeed)
        {
            PanStopCommand = BuildCmd("P50");
            TiltStopCommand = BuildCmd("T50");
            ZoomStopCommand = BuildCmd("Z50");
            PowerOnCommand = BuildCmd("O1");
            PowerOffCommand = BuildCmd("O0");
            HomeCommand = BuildCmd("APC80008000");
            PrivacyCommand = BuildCmd("APC00000000");

            PanSpeed = panSpeed == 0 ? 25 : panSpeed;
            ZoomSpeed = zoomSpeed == 0 ? 25 : zoomSpeed;
            TiltSpeed = tiltSpeed == 0 ? 25 : tiltSpeed;
            InitializeFocus();
        }

        public PanasonicCmdBuilder(int panSpeed, int zoomSpeed, int tiltSpeed, string homeCommand, string privacyCommand)
        {
            PanStopCommand = BuildCmd("P50");
            TiltStopCommand = BuildCmd("T50");
            ZoomStopCommand = BuildCmd("Z50");
            PowerOnCommand = BuildCmd("O1");
            PowerOffCommand = BuildCmd("O0");
            HomeCommand = string.IsNullOrEmpty(homeCommand) ? BuildCmd("APC80008000") : BuildCmd(homeCommand);
            PrivacyCommand = string.IsNullOrEmpty(privacyCommand) ? BuildCmd("APC00000000") : BuildCmd(privacyCommand);

            PanSpeed = panSpeed == 0 ? 25 : panSpeed;
            ZoomSpeed = zoomSpeed == 0 ? 25 : zoomSpeed;
            TiltSpeed = tiltSpeed == 0 ? 25 : tiltSpeed;
            InitializeFocus();
        }

        private void InitializeFocus()
        {
            FocusStopCommand = BuildCmd("F50");
            AutoFocusOnCommand = BuildCmd("D11");
            AutoFocusOffCommand = BuildCmd("D10");
            AutoFocusQueryCommand = BuildCmd("D1");
            OneTouchAutoFocusCommand = OneTouchAutoFocus;
            FocusSpeed = 25;
        }

        private int _panSpeed;
        public int PanSpeed
        {
            get { return _panSpeed; }
            set
            {
                _panSpeed = value <= 0 || value >= 50 ? 25 : value;
                PanLeftCommand = BuildCmd(String.Format("P{0}", 50 - _panSpeed));
                PanRightCommand = BuildCmd(String.Format("P{0}", _panSpeed + 50));
            }
        }

        private int _tiltSpeed;
        public int TiltSpeed
        {
            get { return _tiltSpeed; }
            set
            {
                _tiltSpeed = value <= 0 || value >= 50 ? 25 : value;
                TiltDownCommand = BuildCmd(String.Format("T{0}", 50 - _tiltSpeed));
                TiltUpCommand = BuildCmd(String.Format("T{0}", _tiltSpeed + 50));
            }
        }

        // #F[Data]: 01-49 near (max to min speed), 50 stop, 51-99 far (min to max speed)
        private int _focusSpeed;
        public int FocusSpeed
        {
            get { return _focusSpeed; }
            set
            {
                _focusSpeed = value <= 0 || value >= 50 ? 25 : value;
                FocusNearCommand = BuildCmd(String.Format("F{0}", 50 - _focusSpeed));
                FocusFarCommand = BuildCmd(String.Format("F{0}", _focusSpeed + 50));
            }
        }

        private int _zoomSpeed;
        public int ZoomSpeed
        {
            get { return _zoomSpeed; }
            set
            {
                _zoomSpeed = value <= 0 || value >= 50 ? 25 : value;
                ZoomOutCommand = BuildCmd(String.Format("Z{0}", 50 - _zoomSpeed));
                ZoomInCommand = BuildCmd(String.Format("Z{0}", _zoomSpeed + 50));
            }
        }

        public string PresetRecallCommand(int preset)
        {
            var command = Convert.ToString(preset - 1);
            var formattedCommand = command.PadLeft(2, '0');
			var cmd = BuildCmd(String.Format("R{0}", formattedCommand));			
			Log.Debug("PanasonicCmdBuilder: PresetRecallCommand({preset}) Cmd: {cmd}", preset, cmd);
			return cmd;
        }

        public string PresetSaveCommand(int preset)
        {
			var command = Convert.ToString(preset - 1);
			var formattedCommand = command.PadLeft(2, '0');
			var cmd = BuildCmd(String.Format("M{0}", formattedCommand));
			Log.Debug("PanasonicCmdBuilder: PresetSaveCommand({preset}) Cmd: {cmd}", preset, cmd);
			return cmd;
        }

        /// <summary>
        /// Request for MAC, serial number and firmware version. Not a #-wrapped aw_ptz command.
        /// </summary>
        /// <remarks>
        /// Not in the HD Integrated Camera interface specification; carried over from the earlier DeviceInfo
        /// work and not verified against a camera.
        /// </remarks>
        public const string DeviceInfoCommand = "cgi-bin/getinfo?FILE=1";

        static string BuildCmd(string cmd)
        {
            var builder = new StringBuilder(CmdHeader);
            builder.Append(cmd);
            builder.Append(CmdSuffix);

            return builder.ToString();
        }

        public static string BuildCustomCommand(string cmd)
        {
            return BuildCmd(cmd);
        }
    }
}