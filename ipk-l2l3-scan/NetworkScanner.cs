using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;

public class NetworkScanner
{
    private readonly int timeout;

    private const int pingsAtOnceLimit = 2048;

    public NetworkScanner(int timeout)
    {
        this.timeout = timeout;
    }

    public async Task Scan(Subnet subnet)
    {
        int devicesFound = 0;

        while (!subnet.IsAtMaxIpAddress())
        {
            var tasks = new List<Task<PingReply>>();
            var adresses = new List<IPAddress>();
            for (int i = 0; i < pingsAtOnceLimit; i++)
            {
                if ((subnet++).IsAtMaxIpAddress())
                    break;

                tasks.Add(new Ping().SendPingAsync(subnet.Address, timeout));
                adresses.Add(subnet.Address);
            }
            for (int i = 0; i < tasks.Count; i++)
            {
                var response = await tasks[i];

                if (response.Status == IPStatus.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    devicesFound++;
                }
                Console.WriteLine($"{adresses[i]}:\t{response.Status}");
                Console.ResetColor();
            }
        }
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Found {devicesFound} devices.");
        Console.ResetColor();
    }
}
