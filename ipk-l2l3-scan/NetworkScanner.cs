using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Linq;

public class NetworkScanner
{
    private const int _pingsAtOnceLimit = 2048;
    private readonly int _timeout;
    private readonly IPAddress _selfAddress;
    private readonly ProtocolType _icmpProtocol;
    private readonly char _icmpVersion;
    private Subnet _subnet;

    public NetworkScanner(NetworkInterface @interface, int timeout, Subnet subnet)
    {
        var adresses = @interface.GetIPProperties().UnicastAddresses;
        _selfAddress = adresses.FirstOrDefault(a => a.Address.AddressFamily == subnet.Address.AddressFamily).Address;

        _subnet = subnet;
        _timeout = timeout;
        _icmpProtocol = _selfAddress.AddressFamily == AddressFamily.InterNetwork ? ProtocolType.Icmp : ProtocolType.IcmpV6;
        _icmpVersion = _icmpProtocol == ProtocolType.Icmp ? '4' : '6';
    }

    public async Task ScanAsync()
    {
        var subnetAddressBackup = _subnet.Address;

        while (!_subnet.IsAtMaxIpAddress()) //for each address in subnet
        {
            var tasks = new List<Task<(bool, IPAddress)>>(); //list of ICMP echo tasks
            for (int i = 0; i < _pingsAtOnceLimit; i++) //limit amount of requests at once for large ranges
            {
                if ((_subnet++).IsAtMaxIpAddress()) //incremented subnet IP is not at the end of subnet range
                    break;

                tasks.Add(IcmpEchoAsync(_subnet.Address)); //run ICMP echo task for one IP address
            }
            foreach (var task in tasks) //process all requests
            {
                var result = await task;
                if (result.Item1)
                {
                    Console.ForegroundColor = ConsoleColor.Green; //mark success with green color
                }
                Console.WriteLine($"{result.Item2}:\ticmpv{_icmpVersion} {(result.Item1 ? "OK" : "FAIL")}");
                Console.ResetColor();
            }
        }
        //restore subnet to initial address
        _subnet.Address = subnetAddressBackup;
    }

    private async Task<(bool, IPAddress)> IcmpEchoAsync(IPAddress targetAddress)
    {
        using var timeoutCancellation = new CancellationTokenSource(_timeout);

        using var socket = new Socket(_selfAddress.AddressFamily, SocketType.Raw, _icmpProtocol);
        socket.Bind(new IPEndPoint(_selfAddress, 0));

        try
        {
            await socket.ConnectAsync(new IPEndPoint(targetAddress, 0));
            await socket.SendAsync(IcmpPacket.EchoRequestAsBytes(_icmpProtocol), SocketFlags.None);
        }
        catch //error creating socket - request fails
        { 
            return (false, targetAddress);
        }

        int length;
        try
        {
            var buffer = new byte[32];
            length = await socket.ReceiveAsync(buffer, SocketFlags.None, timeoutCancellation.Token);
        }
        catch (OperationCanceledException) //timeout - request cancelled by token
        {
            length = 0;
        }
        return (length > 0, targetAddress);
    }
}
