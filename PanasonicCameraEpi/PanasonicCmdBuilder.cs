﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PanasonicCameraEpi
{
    public class PanasonicCmdBuilder
    {
        private static readonly string cmd1 = "cgi-bin/aw_ptz?cmd=%23";
        private static readonly string cmd2 = "&res=1";

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

            PanSpeed = panSpeed;
            ZoomSpeed = zoomSpeed;
            TiltSpeed = tiltSpeed;
        }

        private int panSpeed;
        public int PanSpeed
        {
            get { return panSpeed; }
            set
            {
                panSpeed = value.ScaleForCameraControls();
                PanLeftCommand = BuildCmd(String.Format("P{0}", 50 - panSpeed));
                PanRightCommand = BuildCmd(String.Format("P{0}", panSpeed + 50));
            }
        }

        private int tiltSpeed;
        public int TiltSpeed
        {
            get { return tiltSpeed; }
            set
            {
                tiltSpeed = value.ScaleForCameraControls();
                TiltDownCommand = BuildCmd(String.Format("T{0}", 50 - tiltSpeed));
                TiltUpCommand = BuildCmd(String.Format("T{0}", tiltSpeed + 50));
            }
        }

        private int zoomSpeed;
        public int ZoomSpeed
        {
            get { return zoomSpeed; }
            set
            {
                zoomSpeed = value.ScaleForCameraControls();
                ZoomOutCommand = BuildCmd(String.Format("Z{0}", 50 - zoomSpeed));
                ZoomInCommand = BuildCmd(String.Format("Z{0}", zoomSpeed + 50));
            }
        }

        public string PresetRecallCommand(int preset)
        {
            var command = Convert.ToString(preset);
            var formattedCommand = command.PadLeft(2, '0');
            return BuildCmd(String.Format("R{0}", formattedCommand));
        }

        public string PresetSaveCommand(int preset)
        {
            var command = Convert.ToString(preset);
            var formattedCommand = command.PadLeft(2, '0');
            return BuildCmd(String.Format("M{0}", formattedCommand));
        }

        static string BuildCmd(string cmd)
        {
            var builder = new StringBuilder("cgi-bin/aw_ptz?cmd=%23");
            builder.Append(cmd);
            builder.Append("&res=1");

            return builder.ToString();
        }

        public static string BuildCustomCommand(string cmd)
        {
            return BuildCmd(cmd);
        }
    }
}