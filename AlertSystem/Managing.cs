//using Azure.Messaging.EventHubs.Producer;
//using Azure.Messaging.EventHubs;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using System.Diagnostics;
//using System.Management;

//namespace AlertSender
//{
//    class Managing
//    {
//        private const string connectionString = "Endpoint=sb://eventhubkallwik.servicebus.windows.net/;SharedAccessKeyName=Eventpolicy;SharedAccessKey=YMs/K6WRFJY0jZpFBHp8ZhW3NXpAhZ65a+AEhKpf2Zg=;EntityPath=alertevent";
//        private const string eventHubName = "alertevent";

//        static async Task Main()
//        {
//            Console.WriteLine("Monitoring started...");

//            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
//            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

//            while (true)
//            {
//                //float cpuUsage = cpuCounter.NextValue();
//                //await Task.Delay(1000); // wait to stabilize
//                //cpuUsage = cpuCounter.NextValue();

//                await Task.Delay(1000); // wait before first measurement
//                float cpuUsage = cpuCounter.NextValue(); // now accurate


//                float ramAvailable = ramCounter.NextValue();
//                float totalRam = GetTotalRAM();
//                float ramUsage = 100 - (ramAvailable / totalRam * 100);

//                string time = DateTime.Now.ToString("HH:mm:ss");

//                if (cpuUsage > 80 || ramUsage > 80)
//                {
//                    var alert = new
//                    {
//                        Machine = Environment.MachineName,
//                        CPU = Math.Round(cpuUsage, 2),
//                        RAM = Math.Round(ramUsage, 2),
//                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
//                    };

//                    string message = JsonSerializer.Serialize(alert);
//                    await SendToEventHub(message);

//                    Console.ForegroundColor = ConsoleColor.Red;
//                    Console.WriteLine($"[{time}] CPU: {cpuUsage:F2}% | RAM: {ramUsage:F2}% --> 🚨 ALERT Sent!");
//                    Console.ResetColor();
//                }
//                else
//                {
//                    Console.ForegroundColor = ConsoleColor.Green;
//                    Console.WriteLine($"[{time}] CPU: {cpuUsage:F2}% | RAM: {ramUsage:F2}% --> OK");
//                    Console.ResetColor();
//                }

//                await Task.Delay(5000); // wait 5 seconds before next check
//            }
//        }


//        static float GetTotalRAM()
//        {
//            float totalRam = 0;
//            var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
//            foreach (var obj in searcher.Get())
//            {
//                totalRam = Convert.ToSingle(Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024)); // MB
//            }
//            return totalRam;
//        }


//        static async Task SendToEventHub(string message)
//        {
//            await using var producerClient = new EventHubProducerClient(connectionString, eventHubName);
//            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
//            eventBatch.TryAdd(new EventData(message));
//            await producerClient.SendAsync(eventBatch);
//        }
//    }
//}






using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using System;
using System.Diagnostics;
using System.Management;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlertSender
{
    class Managing
    {
        private const string connectionString = "Endpoint=sb://eventhubkallwik.servicebus.windows.net/;SharedAccessKeyName=Eventpolicy;SharedAccessKey=YMs/K6WRFJY0jZpFBHp8ZhW3NXpAhZ65a+AEhKpf2Zg=;EntityPath=alertevent";
        private const string eventHubName = "alertevent";

        // Method 1: Using PerformanceCounter with proper initialization
        private static PerformanceCounter cpuCounter;
        private static PerformanceCounter ramCounter;

        static async Task Main()
        {
            Console.WriteLine("Monitoring started...");

            // Initialize performance counters
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Warm up the CPU counter - take multiple readings to stabilize
            Console.WriteLine("Warming up CPU counter...");
            for (int i = 0; i < 3; i++)
            {
                cpuCounter.NextValue();
                await Task.Delay(1000);
            }
            Console.WriteLine("CPU counter ready!");

            while (true)
            {
                // Method 1: Performance Counter (your current method, improved)
                float cpuUsage1 = await GetCPUUsagePerformanceCounter();

                // Method 2: WMI Query (alternative method)
                float cpuUsage2 = await GetCPUUsageWMI();

                // Method 3: Process.GetCurrentProcess() for this specific process
                float processCpuUsage = GetCurrentProcessCPUUsage();

                // Use the WMI method as it's often more reliable
                float cpuUsage = cpuUsage2;

                float ramAvailable = ramCounter.NextValue();
                float totalRam = GetTotalRAM();
                float ramUsage = 100 - (ramAvailable / totalRam * 100);

                string time = DateTime.Now.ToString("HH:mm:ss");

                if (cpuUsage > 80 || ramUsage > 80)
                {
                    var alert = new
                    {
                        Machine = Environment.MachineName,
                        CPU = Math.Round(cpuUsage, 2),
                        RAM = Math.Round(ramUsage, 2),
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        CPUMethod1 = Math.Round(cpuUsage1, 2), // Performance Counter
                        CPUMethod2 = Math.Round(cpuUsage2, 2), // WMI
                        ProcessCPU = Math.Round(processCpuUsage, 2) // Current process
                    };

                    string message = JsonSerializer.Serialize(alert);
                    await SendToEventHub(message);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{time}] CPU: {cpuUsage:F2}% (WMI) | PerfCounter: {cpuUsage1:F2}% | RAM: {ramUsage:F2}% --> 🚨 ALERT Sent!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{time}] CPU: {cpuUsage:F2}% (WMI) | PerfCounter: {cpuUsage1:F2}% | ProcessCPU: {processCpuUsage:F2}% | RAM: {ramUsage:F2}% --> OK");
                    Console.ResetColor();
                }

                await Task.Delay(5000);
            }
        }

        // Method 1: Improved Performance Counter approach
        static async Task<float> GetCPUUsagePerformanceCounter()
        {
            // Take multiple readings and average them for better accuracy
            float total = 0;
            int readings = 3;

            for (int i = 0; i < readings; i++)
            {
                total += cpuCounter.NextValue();
                if (i < readings - 1) // Don't delay after the last reading
                    await Task.Delay(100);
            }

            return total / readings;
        }

        // Method 2: WMI Query approach (often more reliable)
        static async Task<float> GetCPUUsageWMI()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = 0UL;
                var endCpuUsage = 0UL;

                // Get initial CPU time
                var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    startCpuUsage += Convert.ToUInt64(obj["LoadPercentage"]);
                }

                // Wait a short period
                await Task.Delay(1000);

                // Get CPU usage percentage directly
                searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                float totalUsage = 0;
                int processorCount = 0;

                foreach (var obj in searcher.Get())
                {
                    totalUsage += Convert.ToSingle(obj["LoadPercentage"]);
                    processorCount++;
                }

                return processorCount > 0 ? totalUsage / processorCount : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WMI CPU query failed: {ex.Message}");
                return 0;
            }
        }

        // Method 3: Current process CPU usage
        private static DateTime lastTime = DateTime.MinValue;
        private static TimeSpan lastTotalProcessorTime = TimeSpan.MinValue;

        static float GetCurrentProcessCPUUsage()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentTime = DateTime.UtcNow;
                var currentTotalProcessorTime = currentProcess.TotalProcessorTime;

                if (lastTime != DateTime.MinValue && lastTotalProcessorTime != TimeSpan.MinValue)
                {
                    var timeDiff = currentTime - lastTime;
                    var processorTimeDiff = currentTotalProcessorTime - lastTotalProcessorTime;

                    var cpuUsage = (processorTimeDiff.TotalMilliseconds / timeDiff.TotalMilliseconds) * 100.0 / Environment.ProcessorCount;

                    lastTime = currentTime;
                    lastTotalProcessorTime = currentTotalProcessorTime;

                    return (float)cpuUsage;
                }

                lastTime = currentTime;
                lastTotalProcessorTime = currentTotalProcessorTime;
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process CPU calculation failed: {ex.Message}");
                return 0;
            }
        }

        static float GetTotalRAM()
        {
            float totalRam = 0;
            var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                totalRam = Convert.ToSingle(Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024));
            }
            return totalRam;
        } \

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
                Console.WriteLine($"Failed to send to Event Hub: {ex.Message}");
            }
        }
    }
}

