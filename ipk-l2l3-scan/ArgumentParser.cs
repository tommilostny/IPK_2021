using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Linq;
using System.Net.NetworkInformation;

public static class ArgumentParser
{
    public static async Task<(NetworkInterface, uint, Subnet[])> ParseArguments(string[] args)
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

        (string, uint, string[]) parsedArgs = (null, 0, null);
        rootCommand.Handler = CommandHandler.Create<string, uint, string[]>((@interface, wait, subnet) =>
        {
            parsedArgs = ( @interface, wait, subnet );
        });
        
        if (await rootCommand.InvokeAsync(args) != 0)
            throw new ArgumentException();

        var @interface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == parsedArgs.Item1);
        var subnets = await SubnetParser.ParseSubnets(parsedArgs.Item3);

        return (@interface, parsedArgs.Item2, subnets);
    }
}
