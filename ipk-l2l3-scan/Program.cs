using System;
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
catch (ArgumentNullException) //missing interface exception
{
    Console.WriteLine("Missing or invalid paramerer --interface.");
    PrintAllInterfaces();
    return 0;
}
catch (ApplicationException) //ok exception thrown if help is being displayed
{
    return 0;
}
catch
{
    return 1;
}

Console.WriteLine("Scanning ranges:");
foreach (var subnet in subnets)
{
    Console.WriteLine($"{subnet.Address}/{subnet.MaskLength} ({subnet.HostsCount} hosts)");
}

foreach (var subnet in subnets)
{
    Console.WriteLine();
    var scanner = new NetworkScanner(timeout, @interface, subnet); 
    await scanner.ScanAsync();
}

return 0;
