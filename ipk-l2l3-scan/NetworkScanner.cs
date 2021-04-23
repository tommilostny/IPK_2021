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
    private Subnet _subnet;

    public NetworkScanner(NetworkInterface @interface, int timeout, Subnet subnet)
    {
        var adresses = @interface.GetIPProperties().UnicastAddresses;
        _selfAddress = adresses.FirstOrDefault(a => a.Address.AddressFamily == subnet.Address.AddressFamily).Address;

        _subnet = subnet;
        _timeout = timeout;
        _icmpProtocol = _selfAddress.AddressFamily == AddressFamily.InterNetwork ? ProtocolType.Icmp : ProtocolType.IcmpV6;
    }

    public async Task ScanAsync()
    {
        var subnetAddressBackup = _subnet.Address;

        while (!_subnet.IsAtMaxIpAddress())
        {
            var tasks = new List<Task<(bool, IPAddress)>>();
            for (int i = 0; i < _pingsAtOnceLimit; i++)
            {
                if ((_subnet++).IsAtMaxIpAddress())
                    break;

                tasks.Add(IcmpEchoAsync(_subnet.Address));
            }
            foreach (var task in tasks)
            {
                var result = await task;
                if (result.Item1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                Console.WriteLine($"{result.Item2}:\ticmpv4 {(result.Item1 ? "OK" : "FAIL")}");
                Console.ResetColor();
            }
        }

        _subnet.Address = subnetAddressBackup;
    }

    private async Task<(bool, IPAddress)> IcmpEchoAsync(IPAddress targetAddress)
    {
        using var timeoutCancellation = new CancellationTokenSource(_timeout);

        using var socket = new Socket(_selfAddress.AddressFamily, SocketType.Raw, _icmpProtocol);
        socket.Bind(new IPEndPoint(_selfAddress, 0));

        await socket.ConnectAsync(new IPEndPoint(targetAddress, 0));
        await socket.SendAsync(IcmpPacket.EchoRequestAsBytes(), SocketFlags.None);

        int length;
        try
        {
            var buffer = new byte[128];
            length = await socket.ReceiveAsync(buffer, SocketFlags.None, timeoutCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            length = 0;
        }
        return (length > 0, targetAddress);
    }
}
