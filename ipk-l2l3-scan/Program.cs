using ipk_l2l3_scan;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

static void PrintAllInterfaces()
{
    Console.WriteLine("Available network interfaces:");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("STATUS\tNAME");
    Console.ResetColor();
    foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
    {
        Console.Write($"{@interface.OperationalStatus}\t");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@interface.Name);
        Console.ResetColor();
    }
}

static void ListScanningRanges(Subnet[] subnets)
{
    Console.WriteLine("Scanning ranges:");
    foreach (var subnet in subnets) //list subnet ranges
    {
        var hostsCount = subnet.Address.AddressFamily switch
        {
            AddressFamily.InterNetwork => (uint)Math.Pow(2, 32 - subnet.MaskLength) - 2,
            _ => (uint)Math.Pow(2, 128 - subnet.MaskLength) - 2
        };
        Console.WriteLine($"{subnet.Address}/{subnet.MaskLength} ({hostsCount} hosts)");
    }
}

static async Task ScanNetwork(NetworkInterface @interface, int timeout, Subnet[] subnets)
{
    foreach (var subnet in subnets) //scan all subnets
    {
        Console.WriteLine();
        try
        {
            var scanner = new NetworkScanner(@interface, timeout, subnet);
            await scanner.ScanAsync();
        }
        catch
        {
            Console.Error.WriteLine($"Unable to scan subnet {subnet.Address} on interface {@interface.Name}.");
        }
    }
}

NetworkInterface @interface;
int timeout;
Subnet[] subnets;

try
{
    (@interface, timeout, subnets) = await ArgumentParser.ParseArguments(args);
}
catch (ApplicationException) //ok exception thrown if help is being displayed
{
    return 0;
}
catch (InvalidOperationException) //missing interface exception
{
    Console.WriteLine($"Missing or invalid paramerer --interface.\n");
    PrintAllInterfaces();
    return 0;
}
catch (IndexOutOfRangeException exc) //timeout negative
{
    Console.Error.WriteLine($"{exc.Message}.");
    return 1;
}
catch //other errors printed by ArgumentParser and System.CommandLine
{
    return 1;
}

ListScanningRanges(subnets);
await ScanNetwork(@interface, timeout, subnets);

return 0;