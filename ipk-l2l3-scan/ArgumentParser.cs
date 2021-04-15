using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Linq;
using System.Net.NetworkInformation;

public static class ArgumentParser
{
    public static async Task<(NetworkInterface, uint, string[])> ParseArguments(string[] args)
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
            parsedArgs.Item3 = await SubnetParser.ParseSubnets(parsedArgs.Item3);
        
        return parsedArgs;
    }
}
