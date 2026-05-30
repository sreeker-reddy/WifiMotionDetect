using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XfinityDistanceTracker
{
    class Program
    {
        const string ROUTER_USER = "<username>";       // or "cusadmin"
        const string ROUTER_PASS = "<password>";  // your real password
        const string XFINITY_URL = "<xfinity_connected_devices_url>";

        const string TARGET_HOSTNAME = "iPhone";  // the device you want to track

        private static readonly HttpClient client = new HttpClient(new HttpClientHandler
        {
            Credentials = new NetworkCredential(ROUTER_USER, ROUTER_PASS),
            PreAuthenticate = true
        });

        static int? lastRssi = null;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Xfinity Relative Distance Tracker v8 ===");
            Console.WriteLine($"Tracking RSSI for: {TARGET_HOSTNAME}\n");

            while (true)
            {
                try
                {
                    int? rssi = await GetRssiForDevice(TARGET_HOSTNAME);

                    if (rssi.HasValue)
                        CompareAndPrint(rssi.Value);
                    else
                        Console.WriteLine("iPhone not found in table");

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }

                await Task.Delay(2000);
            }
        }

        // ---------------------------------------------------------
        // Extract RSSI for the target device
        // ---------------------------------------------------------
        static async Task<int?> GetRssiForDevice(string hostname)
        {
            string html = await client.GetStringAsync(XFINITY_URL);

            // Find the row containing the hostname
            string pattern = $@"<tr[^>]*>.*?{hostname}.*?</tr>";
            var rowMatch = Regex.Match(html, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (!rowMatch.Success)
                return null;

            string row = rowMatch.Value;

            // Extract RSSI from the rssi-level column
            var rssiMatch = Regex.Match(row, @"headers='rssi-level'\s*>\s*([-]?\d+)\s*dBm", RegexOptions.IgnoreCase);

            if (!rssiMatch.Success)
                return null;

            return int.Parse(rssiMatch.Groups[1].Value);
        }

        // ---------------------------------------------------------
        // Compare RSSI to determine closer/farther
        // ---------------------------------------------------------
        static void CompareAndPrint(int rssi)
        {
            string trend = "Same";

            if (lastRssi.HasValue)
            {
                if (rssi > lastRssi.Value)
                    trend = "Closer";       // RSSI closer to 0 = stronger signal
                else if (rssi < lastRssi.Value)
                    trend = "Farther";
            }

            Console.WriteLine($"iPhone RSSI: {rssi} dBm → {trend}");

            lastRssi = rssi;
        }
    }
}
