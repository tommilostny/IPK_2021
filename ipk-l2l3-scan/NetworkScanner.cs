using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public class NetworkScanner
{
    private readonly int _timeout;

    private const int _pingsAtOnceLimit = 2048;

    public NetworkScanner(int timeout)
    {
        _timeout = timeout;
    }

    public async Task ScanAsync(Subnet subnet)
    {
        while (!subnet.IsAtMaxIpAddress())
        {
            var tasks = new List<Task>();
            var adresses = new List<string>();

            for (int i = 0; i < _pingsAtOnceLimit; i++)
            {
                if ((subnet++).IsAtMaxIpAddress())
                    break;

                tasks.Add(IcmpRequest(subnet.Address));
                adresses.Add(subnet.Address.ToString());
            }
            await Task.WhenAll(tasks);
        }
    }

    private async Task IcmpRequest(IPAddress address)
    {
        var buffer = new byte[256];
        var protocol = address.AddressFamily == AddressFamily.InterNetwork ? ProtocolType.Icmp : ProtocolType.IcmpV6;
        var endpoint = new IPEndPoint(address, 0);

        //await Console.Out.WriteLineAsync($"{endpoint.Address}:{endpoint.Port}");

        var socket = new Socket(endpoint.AddressFamily, SocketType.Raw, protocol);
        await socket.ConnectAsync(endpoint);
        
        var args = new SocketAsyncEventArgs();
        //event
        var result = socket.ReceiveAsync(args);
        
        //void ReceiveCallback(IAsyncResult result)
        //{
        //    int length = socket.EndReceiveFrom(result, ref endpoint);
        //    if (length > 0)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Green;
        //    }
        //    Console.WriteLine(endpoint.ToString());
        //    Console.ResetColor();
        //}
//
        //socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint, ReceiveCallback, null);
        
        socket.Close(_timeout);
    }
}
