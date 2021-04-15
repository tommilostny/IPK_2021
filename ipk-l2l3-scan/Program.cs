using System;
using System.Linq;
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
string[] subnets;

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

Console.WriteLine($"Interface:\t{@interface.Name}");
Console.WriteLine($"Timeout:\t{timeout} ms");
Console.WriteLine($"Subnets:\t{string.Join(", ", subnets)}");

return 0;
