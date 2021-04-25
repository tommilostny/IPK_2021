using ipk_l2l3_scan;
using System;
using System.Net.NetworkInformation;
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
        Console.WriteLine($"{subnet.Address}/{subnet.MaskLength} ({subnet.HostsCount} hosts)");
    }
}

static async Task ScanNetwork(NetworkInterface @interface, int timeout, Subnet[] subnets)
{
    foreach (var subnet in subnets) //scan all subnets
    {
        Console.WriteLine();
        NetworkScanner scanner;
        try
        {
            scanner = new NetworkScanner(@interface, timeout, subnet);
            await scanner.ScanAsync();
        }
        catch
        {
            Console.Error.WriteLine($"Unable to scan subnet {subnet.Address} on interface {@interface}.");
            continue;  
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
catch (ArgumentNullException exc) //missing interface exception
{
    Console.WriteLine($"{exc.Message}.\n");
    PrintAllInterfaces();
    return 0;
}
catch (IndexOutOfRangeException exc) //timeout negative
{
    Console.Error.WriteLine($"{exc.Message}.");
    return 1;
}
catch
{
    return 1;
}

ListScanningRanges(subnets);
await ScanNetwork(@interface, timeout, subnets);

return 0;