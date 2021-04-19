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
int timeout;
Subnet[] subnets;

try
{
    (@interface, timeout, subnets) = await ArgumentParser.ParseArguments(args);
}
catch (ArgumentNullException)
{
    Console.WriteLine("Missing or invalid paramerer --interface.");
    PrintAllInterfaces();
    return 0;
}
catch
{
    return 1;
}

if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?") || args.Contains("--version"))
    return 0;

Console.WriteLine("Scanning ranges:");
foreach (var subnet in subnets)
{
    Console.WriteLine($"{subnet.Address}/{subnet.MaskLength} ({subnet.HostsCount} hosts)");
}

var scanner = new NetworkScanner(timeout); 
foreach (var subnet in subnets)
{
    Console.WriteLine();
    await scanner.Scan(subnet);
}

return 0;
