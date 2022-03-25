using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PanasonicCameraEpi
{
    public class PanasonicHttpCameraMonitor : StatusMonitorBase
    {
        private readonly CTimer _timer;
        private readonly GenericHttpClient _client;
        private readonly long _pollInterval;
        private readonly string _pollString;

        public PanasonicHttpCameraMonitor(IKeyed parent, GenericHttpClient client,
            CommunicationMonitorConfig props)
            : base (parent, props.TimeToWarning, props.TimeToError)
        {
            _client = client;
            _pollInterval = props.PollInterval;
            _pollString = props.PollString;

            _timer = new CTimer(TimerCallback, props.PollString, Timeout.Infinite, _pollInterval);
            _client.ResponseRecived += HandleResponseReceived;

            CrestronEnvironment.ProgramStatusEventHandler += eventType =>
                {
                    if (eventType != eProgramStatusEventType.Stopping)
                        return;

                    Debug.Console(1, this, "Program stopping, disposing of error timers...");
                    Stop();
                    _timer.Dispose();
                };
        }

        private void HandleResponseReceived(object sender, GenericHttpClientEventArgs e)
        {
            if (e.Error != HTTP_CALLBACK_ERROR.COMPLETED)
                return;

            SetOk();
        }

        public override void Start()
        {
            StartErrorTimers();
            _timer.Reset(0, _pollInterval);
        }

        public override void Stop()
        {
            
            _timer.Stop();
            StopErrorTimers();
        }

        private void TimerCallback(object obj)
        {
            if (string.IsNullOrEmpty(_client.Client.HostName))
            {
                Debug.Console(0, "Panasonic camera hostname not valid");
                return;
            }
            _client.SendText(_pollString);
        }

        private void SetOk()
        {
            Status = MonitorStatus.IsOk;
            ResetErrorTimers();
        }
    }
}