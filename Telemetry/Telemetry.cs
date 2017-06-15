﻿namespace Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.ApplicationInsights;

    public class Telemetry
    {
        private static readonly TelemetryClient Client = new TelemetryClient();
        public string PromptText =
            "Microsoft would like to collect data about how users use Azure IoT samples and some problems they encounter.\r\n" +
            "Microsoft uses this information to improve our tooling experience.\r\n" +
            "Participation is voluntary and when you choose to participate, your device will automatically sends information to Microsoft about how you use Azure IoT samples";

        public Telemetry(string instrumentationKey)
        {
            Client.InstrumentationKey = instrumentationKey;
        }

        private const string SimulatedDevice = "simulated device";
       
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

        public void TrackUserChoice(string choice)
        {
            if (string.IsNullOrEmpty(choice))
            {
                choice = "y";
            }
            Client.TrackEvent("success", new Dictionary<string, string>
            {
                {"language", "C#"},
                {"device", SimulatedDevice},
                {"userchoice", choice}
            });
        }

        public void Track(string eventName, string connectionstring, string projectName, string message)
        {
            if (string.IsNullOrEmpty(Client.InstrumentationKey))
            {
                return;
            }
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
