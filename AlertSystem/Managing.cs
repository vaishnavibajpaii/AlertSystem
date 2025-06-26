using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using System;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlertSystem
{
    class Managing
    {
        private const string connectionString = "Endpoint=sb://eventhubkallwik.servicebus.windows.net/;SharedAccessKeyName=Eventpolicy;SharedAccessKey=YMs/K6WRFJY0jZpFBHp8ZhW3NXpAhZ65a+AEhKpf2Zg=;EntityPath=alertevent";
        private const string eventHubName = "alertevent";
        private static readonly Random random = new Random();

        static async Task Main()
        {
            Console.WriteLine("Monitoring started with random values...\n");

            while (true)
            {
                float cpuUsage = GetRandomUsage();          // 70–100%
                float ramUsage = GetRandomUsage();          // 70–100%
                float ramUsedMB = GetRandomRamUsedMB();     // 4000–8000 MB

                string time = DateTime.Now.ToString("HH:mm:ss");
                string ipAddress = GetLocalIPAddress();
                string user = Environment.UserName;

                var alert = new
                {
                    Machine = Environment.MachineName,
                    IP = ipAddress,
                    User = user,
                    CPU = Math.Round(cpuUsage, 2),
                    RAM = Math.Round(ramUsage, 2),
                    RAMUsedMB = Math.Round(ramUsedMB, 2),
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    PerfCounterCPU = cpuUsage,
                    ProcessCPU = cpuUsage
                };

                string message = JsonSerializer.Serialize(alert);
                await SendToEventHub(message);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"[{time}] CPU: {cpuUsage:F2}% | RAM: {ramUsage:F2}% | IP: {ipAddress} | User: {user}");
                Console.ResetColor();

                await Task.Delay(5000);
            }
        }

        static float GetRandomUsage()
        {
            return (float)(70 + random.NextDouble() * 30);  // 70% to 100%
        }

        static float GetRandomRamUsedMB()
        {
            return (float)(4000 + random.NextDouble() * 4000); // 4000 MB to 8000 MB
        }

        static string GetLocalIPAddress()
        {
            try
            {
                foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
                return "No IPv4";
            }
            catch
            {
                return "IP error";
            }
        }

        static async Task SendToEventHub(string message)
        {
            try
            {
                await using var producerClient = new EventHubProducerClient(connectionString, eventHubName);
                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
                eventBatch.TryAdd(new EventData(message));
                await producerClient.SendAsync(eventBatch);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send to Event Hub: {ex.Message}");
            }
        }
    }
}
