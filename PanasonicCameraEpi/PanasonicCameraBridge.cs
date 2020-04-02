using System;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public static class PanasonicCameraBridge
    {
        public static void LinkToApiExt(this PanasonicCamera camera, Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            var joinMap = new PanasonicCameraJoinMap();

            joinMap.OffsetJoinNumbers(joinStart);

            camera.NameFeedback.LinkInputSig(trilist.StringInput[joinMap.DeviceName]);
            camera.NumberOfPresetsFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfPresets]);
            camera.CameraIsOffFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOff]);
            camera.CameraIsOffFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOn]);
            camera.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline]);
            camera.PanSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.PanSpeed]);
            camera.TiltSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.TiltSpeed]);
            camera.ZoomSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.ZoomSpeed]);

            //camera.ComsFeedback.LinkInputSig(trilist.StringInput[joinMap.DeviceComs]);
            trilist.SetStringSigAction(joinMap.DeviceComs, camera.SendCustomCommand);

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
			trilist.SetSigTrueAction(joinMap.Home, camera.PositionHome);

            trilist.SetUShortSigAction(joinMap.PanSpeed, panSpeed => camera.PanSpeed = panSpeed);
            trilist.SetUShortSigAction(joinMap.ZoomSpeed, zoomSpeed => camera.ZoomSpeed = zoomSpeed);
            trilist.SetUShortSigAction(joinMap.TiltSpeed, tiltSpeed => camera.TiltSpeed = tiltSpeed);

			// TODO: Need to investigate preset recalls.  Commented out foreach loop as it was not working, worked with JTA to get for loop working.
			//		 Preset recalls began working after the trilist.SetSigHeldAction(recallJoin, 5000) and trilist.SetSigTrueAction(saveJoin) were commneted out
	        for (var x = 1; x < 6; x++)
	        {
				Debug.Console(2, "foreach: {1} preset.Key: {0}", x, camera.Key);
				var presetNumber = x;
				var nameJoin = joinMap.PresetNameStart + presetNumber - 1;
				var cameraLocal = camera;
				//preset.Value.LinkInputSig(trilist.StringInput[nameJoin]);

				var recallJoin = joinMap.PresetRecallStart + presetNumber - 1; // 400 + 11 + 1 - 1 = 411
				Debug.Console(2, "foreach: {1} recallJoin: {0}", recallJoin, camera.Key);
				var saveJoin = joinMap.PresetSaveStart + presetNumber - 1;
				trilist.SetSigTrueAction((ushort)recallJoin, () =>
				{
					try
					{
						Debug.Console(2, "{1} cameraLocal.RecallPreset({0})", presetNumber, camera.Key);
						cameraLocal.RecallPreset((int)presetNumber);					
					}
					catch (Exception e)
					{
						Debug.Console(2, Debug.ErrorLogLevel.Error, "{1} cameraLocal.RecallPreset({0}) Exception: {2}", presetNumber, camera.Key, e);
					}
					
				});
				//trilist.SetSigHeldAction((ushort)recallJoin, 5000, () => cameraLocal.SavePreset((int)presetNumber));
				//trilist.SetSigTrueAction((ushort)saveJoin, () => cameraLocal.SavePreset((int)presetNumber));   
	        }
			/*
	        foreach (var preset in camera.PresetNamesFeedbacks)
	        {
				Debug.Console(0, "foreach: preset.Key: {0} preset.Value: {1}", preset.Key, preset.Value);
		        var presetNumber = preset.Key;
		        var nameJoin = joinMap.PresetNameStart + presetNumber - 1;
		        var cameraLocal = camera;
		        preset.Value.LinkInputSig(trilist.StringInput[nameJoin]);

		        var recallJoin = joinMap.PresetRecallStart + presetNumber - 1;
		        var saveJoin = joinMap.PresetSaveStart + presetNumber - 1;
		        trilist.SetSigTrueAction(recallJoin, () =>
		        {
			        cameraLocal.RecallPreset((int) presetNumber);
			        Debug.Console(0, "cameraLocal.RecallPreset({0})", presetNumber);
		        });	       
                trilist.SetSigHeldAction(recallJoin, 5000, () => cameraLocal.SavePreset((int)presetNumber));
                trilist.SetSigTrueAction(saveJoin, () => cameraLocal.SavePreset((int)presetNumber));
            }
			*/
        }
    }
}