using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Serilog;

namespace App.Shared.Helpers
{
    public static class ClientInfoHelper
    {
        /// <summary>
        /// HTTP isteğinden client bilgilerini toplar ve static Log ile kaydeder
        /// </summary>
        /// <param name="httpContext">HttpContext instance</param>
        /// <param name="actionName">İsteğe bağlı action/işlem adı</param>
        /// <param name="additionalMessage">İsteğe bağlı ek mesaj</param>
        public static void LogClientInfo(HttpContext httpContext, string actionName = null, string additionalMessage = null)
        {
            var clientInfo = GetClientInfo(httpContext);

            var message = !string.IsNullOrEmpty(additionalMessage)
                ? $"{additionalMessage} - Client Bilgileri"
                : "Client Bilgileri";

            if (!string.IsNullOrEmpty(actionName))
            {
                message += $" - Action: {actionName}";
            }

            Log.Information(message + " | Host: {Host} | IP: {RemoteIP} | User: {UserName} | Browser: {Browser} | OS: {OperatingSystem} | Device: {DeviceName} | Page: {Page}",
                clientInfo.Host,
                clientInfo.RemoteIPAddress,
                clientInfo.UserName,
                clientInfo.BrowserInfo,
                clientInfo.OperatingSystem,
                clientInfo.DeviceName,
                clientInfo.Page);
        }

        /// <summary>
        /// Client bilgilerini al ve static Log ile özel mesaj yaz
        /// </summary>
        /// <param name="httpContext">HttpContext instance</param>
        /// <param name="message">Log mesajı</param>
        /// <param name="parameters">Ek parametreler</param>
        public static void LogWithClientInfo(HttpContext httpContext, string message, params object[] parameters)
        {
            var clientInfo = GetClientInfo(httpContext);

            // Mevcut parametrelere client bilgilerini ekle
            var allParams = new List<object>(parameters ?? new object[0])
            {
                clientInfo.RemoteIPAddress,
                clientInfo.BrowserInfo,
                clientInfo.UserAgent
            };

            Log.Information(message + " | IP: {IP} | Browser: {Browser} | UserAgent: {UserAgent}", allParams.ToArray());
        }

        /// <summary>
        /// HTTP isteğinden client bilgilerini toplar ve ClientInfo objesi döner
        /// </summary>
        /// <param name="httpContext">HttpContext instance</param>
        /// <returns>Client bilgilerini içeren ClientInfo objesi</returns>
        public static ClientInfo GetClientInfo(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return new ClientInfo { Error = "HttpContext bulunamadı" };
            }

            var clientInfo = new ClientInfo();

            try
            {
                // Temel bilgiler
                clientInfo.Host = httpContext.Request.Host.Value ?? "";
                clientInfo.Page = httpContext.Request.Path.Value ?? "";

                // IP Address
                clientInfo.RemoteIPAddress = GetClientIPAddress(httpContext);
                clientInfo.UserName = httpContext.User?.Identity?.Name ?? "Anonymous";

                // Browser ve User Agent bilgileri
                var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";
                clientInfo.UserAgent = userAgent;
                clientInfo.BrowserInfo = ParseBrowserFromUserAgent(userAgent);

                // İşletim sistemi bilgisi
                clientInfo.OperatingSystem = GetOperatingSystemFromUserAgent(userAgent);

                // Route bilgileri
                var routeData = httpContext.GetRouteData();
                if (routeData != null)
                {
                    clientInfo.ControllerName = routeData.Values["controller"]?.ToString() ?? "";
                    clientInfo.ActionName = routeData.Values["action"]?.ToString() ?? "";
                }

                // Timestamp
                clientInfo.Timestamp = DateTime.Now;

                // Device name (performans için opsiyonel)
                clientInfo.DeviceName = "N/A"; // DNS lookup'ı kapatıyoruz performans için
            }
            catch (Exception ex)
            {
                clientInfo.Error = ex.Message;
            }

            return clientInfo;
        }

        private static string GetClientIPAddress(HttpContext httpContext)
        {
            // X-Forwarded-For header'ından al (Proxy/Load Balancer)
            var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                string[] addresses = xForwardedFor.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0].Trim();
                }
            }

            // X-Real-IP header'ından dene
            var xRealIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp.Trim();
            }

            // CF-Connecting-IP (Cloudflare)
            var cfConnectingIp = httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(cfConnectingIp))
            {
                return cfConnectingIp.Trim();
            }

            // Connection RemoteIpAddress
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
            if (remoteIpAddress != null)
            {
                if (remoteIpAddress.IsIPv4MappedToIPv6)
                {
                    return remoteIpAddress.MapToIPv4().ToString();
                }
                return remoteIpAddress.ToString();
            }

            return "Bilinmiyor";
        }

        private static string ParseBrowserFromUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Bilinmiyor";

            if (userAgent.Contains("Chrome")) return "Chrome";
            if (userAgent.Contains("Firefox")) return "Firefox";
            if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
            if (userAgent.Contains("Edge")) return "Edge";
            if (userAgent.Contains("Opera")) return "Opera";
            if (userAgent.Contains("MSIE") || userAgent.Contains("Trident")) return "Internet Explorer";

            return "Diğer";
        }

        private static string GetOperatingSystemFromUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Bilinmiyor";

            if (userAgent.Contains("Windows NT 10.0")) return "Windows 10";
            if (userAgent.Contains("Windows NT 6.3")) return "Windows 8.1";
            if (userAgent.Contains("Windows NT 6.2")) return "Windows 8";
            if (userAgent.Contains("Windows NT 6.1")) return "Windows 7";
            if (userAgent.Contains("Windows")) return "Windows";
            if (userAgent.Contains("Mac OS X")) return "macOS";
            if (userAgent.Contains("Linux")) return "Linux";
            if (userAgent.Contains("Android")) return "Android";
            if (userAgent.Contains("iOS") || userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";

            return "Bilinmiyor";
        }
    }


    /// <summary>
    /// Client bilgilerini tutacak model sınıfı
    /// </summary>
    public class ClientInfo
    {
        public string Host { get; set; } = "";
        public string Page { get; set; } = "";
        public string RemoteIPAddress { get; set; } = "";
        public string OperatingSystem { get; set; } = "";
        public string BrowserType { get; set; } = "";
        public string BrowserPlatformName { get; set; } = "";
        public string BrowserInfo { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ControllerName { get; set; } = "";
        public string ActionName { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Error { get; set; } = "";

        public override string ToString()
        {
            return $"Host: {Host}, IP: {RemoteIPAddress}, User: {UserName}, Browser: {BrowserInfo}, OS: {OperatingSystem}";
        }
    }
}
