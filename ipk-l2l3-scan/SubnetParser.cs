using System.Threading.Tasks;
using System.Collections.Generic;

public static class SubnetParser
{
    public static async Task<string[]> ParseSubnets(string[] subnets)
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

    private static string ParseSubnet(string subnet)
    {
        return subnet;
    }
}
