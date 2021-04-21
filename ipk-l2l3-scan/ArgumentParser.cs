using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Linq;
using System.Net.NetworkInformation;

public static class ArgumentParser
{
    public static async Task<(NetworkInterface, int, Subnet[])> ParseArguments(string[] args)
    {
        var interfaceOption = new Option<string>
        (
            aliases: new string[] { "--interface", "-i" }
        );
        var waitOption = new Option<int>
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

        (string, int, string[]) parsedArgs = (null, 0, null);
        rootCommand.Handler = CommandHandler.Create<string, int, string[]>((@interface, wait, subnet) =>
        {
            parsedArgs = ( @interface, wait, subnet );
        });
        
        if (await rootCommand.InvokeAsync(args) != 0)
            throw new ArgumentException();

        if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?") || args.Contains("--version"))
            throw new ApplicationException();

        var @interface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == parsedArgs.Item1);        
        if (@interface is null)
            throw new ArgumentNullException();
        
        var subnets = await SubnetParser.ParseSubnets(parsedArgs.Item3);

        return (@interface, parsedArgs.Item2, subnets);
    }
}
