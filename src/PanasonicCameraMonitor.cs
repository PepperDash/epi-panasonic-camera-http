using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Serilog.Events;

namespace PanasonicCameraEpi
{
    public class PanasonicHttpCameraMonitor : StatusMonitorBase
    {
        private readonly CTimer _timer;
        private readonly HttpClient _client;
        private readonly long _pollInterval;
        private readonly string _pollString;
        private readonly string _hostname;

        public PanasonicHttpCameraMonitor(IKeyed parent, string hostname,
            CommunicationMonitorConfig props)
            : base (parent, props.TimeToWarning, props.TimeToError)
        {
            _hostname = hostname;
            _client = new HttpClient();
            _pollInterval = props.PollInterval;
            _pollString = props.PollString;

            _timer = new CTimer(TimerCallback, props.PollString, Timeout.Infinite, _pollInterval);

            CrestronEnvironment.ProgramStatusEventHandler += eventType =>
                {
                    if (eventType != eProgramStatusEventType.Stopping)
                        return;

                    Debug.LogMessage(LogEventLevel.Information, this, "Program stopping, disposing of error timers...");
                    Stop();
                    _timer.Dispose();
                    _client?.Dispose();
                };
        }

        private void HandleHttpResponse(HttpClientResponse response, HTTP_CALLBACK_ERROR error)
        {
            if (error == HTTP_CALLBACK_ERROR.COMPLETED && response.Code == 200)
            {
                SetOk();
            }
            else
            {
                Debug.LogMessage(LogEventLevel.Warning, this, "HTTP request failed. Error: {0}, Code: {1}", error, response?.Code);
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
            if (string.IsNullOrEmpty(_hostname))
            {
                Debug.LogMessage(LogEventLevel.Error, "PanasonicCameraMonitor", "Panasonic camera hostname not valid");
                return;
            }

            try
            {
                var request = new HttpClientRequest
                {
                    Url = new UrlParser($"http://{_hostname}/{_pollString}"),
                    RequestType = RequestType.Get
                };

                _client.DispatchAsync(request, HandleHttpResponse);
            }
            catch (System.Exception ex)
            {
                Debug.LogMessage(LogEventLevel.Error, this, "Error sending HTTP request: {0}", ex.Message);
            }
        }

        private void SetOk()
        {
            Status = MonitorStatus.IsOk;
            ResetErrorTimers();
        }
    }
}