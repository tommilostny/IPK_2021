using System;
using System.Net.NetworkInformation;

void PrintAllInterfaces()
{
    Console.WriteLine("Available network interfaces:\n");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("STATUS\tTYPE\tNAME");
    Console.ResetColor();
    foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
    {
        Console.Write($"{@interface.OperationalStatus}\t{@interface.NetworkInterfaceType}\t");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@interface.Name);
        Console.ResetColor();
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
    var scanner = new NetworkScanner(@interface, timeout, subnet); 
    await scanner.ScanAsync();
}

return 0;
