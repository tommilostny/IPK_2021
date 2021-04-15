using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System;

public static class SubnetParser
{
    public static async Task<Subnet[]> ParseSubnets(string[] subnets)
    {
        var tasks = new List<Task<Subnet>>();
        foreach (var subnet in subnets)
        {
            tasks.Add(Task.Run(() => ParseSubnet(subnet)));
        }
        var parsedSubnets = new Subnet[subnets.Length];
        for (int i = 0; i < parsedSubnets.Length; i++)
        {
            parsedSubnets[i] = await tasks[i];
        }
        return parsedSubnets;
    }

    private static Subnet ParseSubnet(string subnet)
    {
        try
        { 
            var parts = subnet.Split("/");
            return new Subnet(IPAddress.Parse(parts[0]), Convert.ToUInt16(parts[1]));
        }
        catch (Exception exc)
        {
            Console.Error.WriteLine($"Invalid subnet {subnet}: {exc.Message}");
            return null;
        }
    }
}
