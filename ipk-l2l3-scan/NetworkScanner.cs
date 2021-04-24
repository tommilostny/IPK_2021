using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ipk_l2l3_scan
{
    public class NetworkScanner
    {
        private const int _pingsAtOnceLimit = 1024;
        private readonly int _timeout;
        private readonly IPAddress _selfAddress;
        private readonly ProtocolType _icmpProtocol;
        private readonly char _icmpVersion;
        private Subnet _subnet;
        private readonly NetworkInterface _interface;

        public NetworkScanner(NetworkInterface @interface, int timeout, Subnet subnet)
        {
            var adresses = @interface.GetIPProperties().UnicastAddresses;
            _selfAddress = adresses.FirstOrDefault(a => a.Address.AddressFamily == subnet.Address.AddressFamily).Address;

            _interface = @interface;
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
                var tasks = new List<Task<(bool, IPAddress, bool, PhysicalAddress)>>(); //list of ICMP echo tasks
                for (int i = 0; i < _pingsAtOnceLimit; i++) //limit amount of requests at once for large ranges
                {
                    if (_subnet++.IsAtMaxIpAddress()) //incremented subnet IP is not at the end of subnet range
                        break;

                    tasks.Add(EchoAsync(_subnet.Address)); //run ICMP echo task for one IP address
                }
                foreach (var task in tasks) //process all requests
                {
                    var result = await task;
                    if (result.Item1)
                    {
                        Console.ForegroundColor = ConsoleColor.Green; //mark ICMP success with green color
                    }
                    Console.WriteLine($"{result.Item2}:\tarp {(result.Item3 ? $"OK ({result.Item4})" : "FAIL")}, icmpv{_icmpVersion} {(result.Item1 ? "OK" : "FAIL")}");
                    Console.ResetColor();
                }
            }
            //restore subnet to initial address
            _subnet.Address = subnetAddressBackup;
        }

        private async Task<(bool, IPAddress, bool, PhysicalAddress)> EchoAsync(IPAddress targetAddress)
        {
            var resultMacAddress = PhysicalAddress.None;
            using var timeoutCancellation = new CancellationTokenSource(_timeout);

            using var icmpSocket = new Socket(_selfAddress.AddressFamily, SocketType.Raw, _icmpProtocol);
            icmpSocket.Bind(new IPEndPoint(_selfAddress, 0));

            try
            {
                await icmpSocket.ConnectAsync(new IPEndPoint(targetAddress, 0));
                await icmpSocket.SendAsync(IcmpPacket.EchoRequestAsBytes(_icmpProtocol), SocketFlags.None);
            }
            catch //error creating socket - request fails
            {
                return (false, targetAddress, false, resultMacAddress);
            }

            int icmpLength;
            try
            {
                var buffer = new byte[32];
                icmpLength = await icmpSocket.ReceiveAsync(buffer, SocketFlags.None, timeoutCancellation.Token);
            }
            catch (OperationCanceledException) //timeout - request cancelled by token
            {
                icmpLength = 0;
            }

            //ICMP successful, continue to ARP for IPv4
            bool arpSuccess = false;
            if (icmpLength > 0 && _selfAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                try
                {
                    resultMacAddress = SendArp(targetAddress);
                    arpSuccess = resultMacAddress.GetAddressBytes().Any(mac => mac != 0); //00-00-00-00-00-00 -> empty MAC, fail
                }
                catch
                {
                    resultMacAddress = PhysicalAddress.None;
                }
            }
            return (icmpLength > 0, targetAddress, arpSuccess, resultMacAddress);
        }

        private PhysicalAddress SendArp(IPAddress targetAddress) //get target MAC address using SharpPcap library
        {
            var device = LibPcapLiveDeviceList.Instance.First(i => i.Interface.FriendlyName == _interface.Name);

            if (targetAddress.Equals(_selfAddress))
                return _interface.GetPhysicalAddress();

            var arp = new ARP(device)
            {
                Timeout = new TimeSpan(0, 0, 0, 0, _timeout)
            };
            return arp.Resolve(targetAddress);
        }
    }
}