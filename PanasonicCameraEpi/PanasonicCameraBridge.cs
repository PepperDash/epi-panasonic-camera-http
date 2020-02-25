using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public static class PanasonicCameraBridge
    {
        public static void LinkToApiExt(this PanasonicCamera camera, Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            var joinMap = new PanasonicCameraJoinMap(joinStart);

            camera.NameFeedback.LinkInputSig(trilist.StringInput[joinMap.DeviceName]);
            camera.NumberOfPresetsFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfPresets]);
            camera.CameraIsOffFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOff]);
            camera.CameraIsOffFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOn]);
            camera.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline]);

            foreach (var preset in camera.PresetNamesFeedbacks)
            {
                var presetNumber = preset.Key;
                var nameJoin = joinMap.PresetNameStart + presetNumber - 1;
                preset.Value.LinkInputSig(trilist.StringInput[nameJoin]);

                var recallJoin = joinMap.PresetRecallStart + presetNumber - 1;
                var saveJoin = joinMap.PresetSaveStart + presetNumber - 1;
                trilist.SetSigTrueAction(recallJoin, () => camera.RecallPreset((int)presetNumber));
                trilist.SetSigHeldAction(recallJoin, 5000, () => camera.SavePreset((int)presetNumber));
                trilist.SetSigTrueAction(saveJoin, () => camera.SavePreset((int)presetNumber));
            }

            camera.ComsFeedback.LinkInputSig(trilist.StringInput[joinMap.DeviceComs]);
            trilist.SetStringSigAction(joinMap.DeviceComs, cmd => camera.SendCustomCommand(cmd));

            trilist.SetBoolSigAction(joinMap.PanLeft, sig =>
                {
                    if (sig) camera.PanLeft();
                    else camera.PanStop();
                });
            trilist.SetBoolSigAction(joinMap.PanRight, sig =>
                {
                    if (sig) camera.PanRight();
                    else camera.PanStop();
                });
            trilist.SetBoolSigAction(joinMap.TiltUp, sig =>
                {
                    if (sig) camera.TiltUp();
                    else camera.TiltStop();
                });
            trilist.SetBoolSigAction(joinMap.TiltDown, sig =>
                {
                    if (sig) camera.TiltDown();
                    else camera.TiltStop();
                });
            trilist.SetBoolSigAction(joinMap.ZoomIn, sig =>
                {
                    if (sig) camera.ZoomIn();
                    else camera.ZoomStop();
                });
            trilist.SetBoolSigAction(joinMap.ZoomOut, sig =>
                {
                    if (sig) camera.ZoomOut();
                    else camera.ZoomStop();
                });

            trilist.SetSigTrueAction(joinMap.PowerOn, camera.CameraOn);
            trilist.SetSigTrueAction(joinMap.PowerOff, camera.CameraOff);
            trilist.SetSigTrueAction(joinMap.PrivacyOn, camera.PositionPrivacy);
            trilist.SetSigTrueAction(joinMap.PrivacyOff, camera.PositionHome);

            trilist.SetUShortSigAction(joinMap.PanSpeed, panSpeed => camera.PanSpeed = panSpeed);
            trilist.SetUShortSigAction(joinMap.ZoomSpeed, zoomSpeed => camera.ZoomSpeed = zoomSpeed);
            trilist.SetUShortSigAction(joinMap.TiltSpeed, tiltSpeed => camera.TiltSpeed = tiltSpeed);
        }
    }
}