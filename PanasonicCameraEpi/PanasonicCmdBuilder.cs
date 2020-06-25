using System;
using System.Text;
using PepperDash.Core;

namespace PanasonicCameraEpi
{
    public class PanasonicCmdBuilder
    {
        private static readonly string CmdHeader = "cgi-bin/aw_ptz?cmd=%23";
        private static readonly string CmdSuffix = "&res=1";

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
			Debug.Console(2, "PresetRecallCommand({0}) Cmd: {1}", preset, cmd);
			return cmd;
        }

        public string PresetSaveCommand(int preset)
        {
			var command = Convert.ToString(preset - 1);
			var formattedCommand = command.PadLeft(2, '0');
			var cmd = BuildCmd(String.Format("M{0}", formattedCommand));
			Debug.Console(2, "PresetSaveCommand({0}) Cmd: {1}", preset, cmd);
			return cmd;
        }

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