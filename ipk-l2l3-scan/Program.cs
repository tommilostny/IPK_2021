using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

void PrintAllInterfaces()
{
    Console.WriteLine("Available network interfaces:\n");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("NAME\tSTATUS\tTYPE");
    Console.ResetColor();
    foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(@interface.Name);
        Console.ResetColor();
        Console.WriteLine($"\t{@interface.OperationalStatus}\t{@interface.NetworkInterfaceType}");
    }
}

NetworkInterface @interface;
uint timeout;
Subnet[] subnets;

try
{
    (@interface, timeout, subnets) = await ArgumentParser.ParseArguments(args);
}
catch { return 1; }

if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?") || args.Contains("--version"))
    return 0;

if (@interface is null)
{
    Console.WriteLine("Missing or invalid paramerer --input.");
    PrintAllInterfaces();
    return 0;
}
if (subnets.Any(s => s is null))
    return 1;

Console.WriteLine($"Interface:\t{@interface.Name}");
Console.WriteLine($"Timeout:\t{timeout} ms");
Console.WriteLine($"Subnets:");
foreach (var subnet in subnets)
{
    Console.Write($"\t{subnet.IP} with");
    if (subnet.Maskv6 is not null)
    {
        Console.WriteLine($" IPv6 mask {Convert.ToString((long)subnet.Maskv6[0], 2)} {Convert.ToString((long)subnet.Maskv6[1], 2)}");
        continue;
    }

    var iterations = Math.Pow(2, (subnet.IP.AddressFamily == AddressFamily.InterNetwork ? 32 : 128) - subnet.MaskLength);
    for (int i = 0; i < iterations; i++)
    {
        if (subnet.Maskv4 is not null)
        {
            Console.WriteLine($" IPv4  {Convert.ToString((uint)subnet.Maskv4, 2)} -> {IPAddress.Parse(subnet.Maskv4.ToString())}");
            subnet.Maskv4++;
        }   
    }
}

return 0;
