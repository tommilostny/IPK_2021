using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.NetworkInformation;


async Task<(NetworkInterface, uint, string[])> ParseArguments(string[] args)
{
    var interfaceOption = new Option<string>
    (
        aliases: new string[] { "--interface", "-i" }
    );
    var waitOption = new Option<uint>
    (
        aliases: new string[] { "--wait", "-w" },
        getDefaultValue:() => 5000
    );
    var subnetOptions = new Option<string[]>
    (
        aliases: new string[] { "--subnet", "-s" }
    );
    subnetOptions.IsRequired = args.Contains("--interface") || args.Contains("-i");

    var rootCommand = new RootCommand { interfaceOption, waitOption, subnetOptions };
    rootCommand.Description = "IPK Project 2\nDELTA variant: Network availability scanner";

    (NetworkInterface, uint, string[]) parsedArgs = (null, 0, null);
    rootCommand.Handler = CommandHandler.Create<string, uint, string[]>((@interface, wait, subnet) =>
    {
        parsedArgs = (
            NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == @interface),
            wait,
            subnet
        );
    });
    
    if (await rootCommand.InvokeAsync(args) != 0)
        throw new ArgumentException();

    if (parsedArgs.Item3 is not null && parsedArgs.Item1 is not null)
        parsedArgs.Item3 = await ParseSubnets(parsedArgs.Item3);
    
    return parsedArgs;
}

async Task<string[]> ParseSubnets(string[] subnets)
{
    var tasks = new List<Task<string>>();
    foreach (var subnet in subnets)
    {
        tasks.Add(Task.Run(() => ParseSubnet(subnet)));
    }
    var parsedSubnets = new string[tasks.Count];
    for (int i = 0; i < parsedSubnets.Length; i++)
    {
        parsedSubnets[i] = await tasks[i];
    }
    return parsedSubnets;
}

string ParseSubnet(string subnet)
{
    return subnet;
}

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
    (@interface, timeout, subnets) = await ParseArguments(args);
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
