﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.NetworkInformation;


async Task<(NetworkInterface, uint, string[])> ParseArguments(string[] args)
{
    var interfaceOption = new Option<string>  (new string[] { "--interface", "-i" });
    var waitOption      = new Option<uint>    (new string[] { "--wait", "-w" }, getDefaultValue:() => 5000);
    var subnetOptions   = new Option<string[]>(new string[] { "--subnet", "-s" });

    subnetOptions.IsRequired = args.Contains("--interface") || args.Contains("-i");

    var rootCommand = new RootCommand { interfaceOption, waitOption, subnetOptions };
    rootCommand.Description = "IPK Project 2\nDELTA variant: Network availability scanner";

    (NetworkInterface, uint, string[]) parsedArgs = (null, 0, null);
    rootCommand.Handler = CommandHandler.Create<string, uint, string[]>((@interface, wait, subnet) =>
    {
        parsedArgs = (
            NetworkInterface.GetAllNetworkInterfaces().SingleOrDefault(i => i.Name == @interface),
            wait,
            subnet);
    });
    
    if (await rootCommand.InvokeAsync(args) != 0)
        throw new ArgumentException();

    return parsedArgs;
}

void PrintAllInterfaces()
{
    var allInterfaces = new List<string>();
    foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
    {
        allInterfaces.Add(@interface.Name);
    }
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(string.Join('\n', allInterfaces));
    Console.ResetColor();
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
    Console.WriteLine("Missing or invalid --interface argument, here are available interfaces:");
    PrintAllInterfaces();
    return 0;
}

Console.WriteLine($"Interface:\t{@interface.Name}, {@interface.OperationalStatus}, {@interface.NetworkInterfaceType}, {@interface.Speed} bps");
Console.WriteLine($"Timeout:\t{timeout} ms");
Console.WriteLine($"Subnets:\t{string.Join(", ", subnets)}");

return 0;
