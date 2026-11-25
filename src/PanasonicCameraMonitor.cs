using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Core.Logging;

namespace PanasonicCameraEpi
{
    public class PanasonicHttpCameraMonitor : StatusMonitorBase, IDisposable
    {
        private readonly CTimer _timer;
        private readonly HttpCommandQueue _httpQueue;
        private readonly long _pollInterval;
        private readonly string _pollString;
        private bool _disposed;

        public PanasonicHttpCameraMonitor(IKeyed parent, string hostname,
            CommunicationMonitorConfig props)
            : base (parent, props.TimeToWarning, props.TimeToError)
        {
            _httpQueue = new HttpCommandQueue(hostname, parent.Key + "-monitor", 0);
            _httpQueue.ResponseReceived += HandleHttpResponse;
            _pollInterval = props.PollInterval;
            _pollString = props.PollString;

            _timer = new CTimer(TimerCallback, props.PollString, Timeout.Infinite, _pollInterval);

            CrestronEnvironment.ProgramStatusEventHandler += eventType =>
                {
                    if (eventType != eProgramStatusEventType.Stopping)
                        return;

                    this.LogInformation("Program stopping, disposing of error timers...");
                    Dispose();
                };
        }

        /// <summary>
        /// Host that status polls are sent to; may be changed while running.
        /// </summary>
        public string Hostname
        {
            get { return _httpQueue.Hostname; }
            set { _httpQueue.Hostname = value; }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
            _timer.Dispose();
            _httpQueue.Dispose();
        }

        private void HandleHttpResponse(object sender, HttpResponse response)
        {
            if (response != null && response.StatusCode == (int)System.Net.HttpStatusCode.OK)
            {
                SetOk();
            }
            else
            {
                this.LogWarning("HTTP request failed. Status Code: {0}", response?.StatusCode ?? 0);
            }
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
            try
            {
                _httpQueue.EnqueueCmd(_pollString);
            }
            catch (System.Exception ex)
            {
                this.LogError("Error sending HTTP request: {0}", ex.Message);
            }
        }

        private void SetOk()
        {
            Status = MonitorStatus.IsOk;
            ResetErrorTimers();
        }
    }
}