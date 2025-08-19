using System;
using System.Net;
using System.IO;
using PepperDash.Core;
using PepperDash.Core.Logging;

namespace PanasonicCameraEpi
{
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public string Content { get; set; }
    }

    public class HttpRequestData
    {
        public string Path { get; set; }
        public string Method { get; set; }
    }

    public class HttpCommandQueue : IKeyed, IDisposable
    {
        public event EventHandler<HttpResponse> ResponseReceived;
        public event EventHandler<string> StatusResponseReceived;
        private readonly string hostname;

        public string Key { get; private set; }
        public bool Disposed { get; private set; }

        public HttpCommandQueue(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
                throw new ArgumentException("Hostname cannot be null or empty", nameof(hostname));
                
            this.hostname = hostname;
            Key = $"http-{hostname}";
        }

        public HttpCommandQueue(string hostname, string parentDeviceKey)
        {
            if (string.IsNullOrEmpty(hostname))
                throw new ArgumentException("Hostname cannot be null or empty", nameof(hostname));
            
            if (string.IsNullOrEmpty(parentDeviceKey))
                throw new ArgumentException("Parent device key cannot be null or empty", nameof(parentDeviceKey));
                
            this.hostname = hostname;
            Key = $"http-{parentDeviceKey}";
        }
        

        public void EnqueueCmd(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            var requestData = new HttpRequestData
            {
                Path = path,
                Method = "GET"
            };

            this.LogDebug("Sending {0} request to: {1}", requestData.Method, path);

            try
            {
                var response = SendHttpRequest(requestData);
                if (response != null)
                {
                    this.LogDebug("Received HTTP response: {0}", response.Content);
                    OnResponseReceived(response);
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                this.LogWarning("Device at {0} is not reachable (network error): {1}", hostname, ex.Message);
                return;
            }
            catch (WebException ex)
            {
                this.LogWarning("Device at {0} web request failed: {1}", hostname, ex.Message);
                return;
            }
            catch (Exception ex)
            {
                this.LogError("HTTP request failed for path '{0}': {1}", path, ex.Message);
                this.LogDebug("HTTP request exception details: {0}", ex.StackTrace);
                throw;
            }
        }
        
        private HttpResponse SendHttpRequest(HttpRequestData requestData)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(HttpCommandQueue));
                
            var url = $"http://{hostname}/{requestData.Path}";
            var request = (HttpWebRequest)WebRequest.Create(url);
            
            // Configure connection management
            request.Method = requestData.Method;
            request.Timeout = 10000; // 10 second timeout
            request.ReadWriteTimeout = 10000; // 10 second read/write timeout
            request.KeepAlive = false; // Disable keep-alive for simpler connection management
            request.ProtocolVersion = HttpVersion.Version11;
            request.ServicePoint.ConnectionLimit = 10; // Limit concurrent connections
            request.ServicePoint.MaxIdleTime = 30000; // 30 second idle timeout
            
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    return new HttpResponse
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = content
                    };
                }
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse errorResponse)
            {
                try
                {
                    using (var stream = errorResponse.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        return new HttpResponse
                        {
                            StatusCode = (int)errorResponse.StatusCode,
                            Content = content
                        };
                    }
                }
                finally
                {
                    errorResponse?.Close();
                }
            }
            finally
            {
                response?.Close();
            }
        }
        
        private void OnResponseReceived(HttpResponse response)
        {
            try
            {
                if (response == null)
                {
                    this.LogWarning("Panasonic camera callback received null response - device may be unreachable");
                    return;
                }

                this.LogInformation("Panasonic camera response code: {0}", response.StatusCode);
                if (response.StatusCode < 200 || response.StatusCode >= 300)
                {
                    this.LogWarning("Panasonic camera callback http code error: {0}", response.StatusCode);
                    return;
                }

                ResponseReceived?.Invoke(this, response);

            }
            catch (Exception ex)
            {
                this.LogError("Panasonic camera callback exception: {0}", ex.Message);
                this.LogDebug("Exception details: {0}", ex.StackTrace);
            }
        }
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;
                
            if (disposing)
            {
                // Clean up managed resources
                try
                {
                    // Close any open service point connections
                    var servicePoint = ServicePointManager.FindServicePoint(new Uri($"http://{hostname}"));
                    servicePoint?.CloseConnectionGroup("");
                }
                catch (Exception ex)
                {
                    this.LogDebug("Error closing service point connections: {0}", ex.Message);
                }
            }
            
            Disposed = true;
        }
        
        ~HttpCommandQueue()
        {
            Dispose(false);
        }
        
        #endregion
        
    }

}