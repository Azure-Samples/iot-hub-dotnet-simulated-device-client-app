namespace Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.ApplicationInsights;

    public sealed class Telemetry
    {
        private static readonly Lazy<Telemetry> Lazy = new Lazy<Telemetry>(() => new Telemetry());

        public static Telemetry Instance => Lazy.Value;

        private const string SimulatedDevice = "simulated device";

        private static readonly TelemetryClient Client = new TelemetryClient
        {
            InstrumentationKey = "0823bae8-a3b8-4fd5-80e5-f7272a2377a9"
        };

        private static string SHA256Hash(string value)
        {
            using (var hash = SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(item => item.ToString("x2")));
            }
        }

        private static string GetMac()
        {
            return
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(p => p.OperationalStatus == OperationalStatus.Up)
                    .Select(p => p.GetPhysicalAddress().ToString())
                    .FirstOrDefault();
        }

        public void Track(string eventName, string connectionstring, string projectName, string message)
        {
            try
            {
                Client.TrackEvent(eventName, new Dictionary<string, string>
                {
                    {"iothub", SHA256Hash(connectionstring.Split('.').FirstOrDefault())},
                    {"language", "C#"},
                    {"device", SimulatedDevice},
                    {"project_name", projectName},
                    {"osversion", Environment.OSVersion.ToString()},
                    {"mac", SHA256Hash(GetMac())},
                    {"message", message}
                });
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
