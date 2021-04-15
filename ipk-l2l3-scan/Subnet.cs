using System;
using System.Net;
using System.Net.Sockets;

#nullable enable

public class Subnet
{
    public IPAddress IP { get; }

    public uint? Maskv4 { get; set; } = null;

    public ulong[]? Maskv6 { get; set; } = null;

    public ushort MaskLength { get; }

    public Subnet(IPAddress ip, ushort maskLength)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            if (maskLength > 32)
                throw new ArgumentOutOfRangeException($"Invalid IP mask length: {maskLength}.");

            Maskv4 = 0U;
            for (ushort i = 0; i < maskLength; i++)
            {
                Maskv4 = (Maskv4 >> 1) | 0x8000_0000U;
            }
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (maskLength > 128)
                throw new ArgumentOutOfRangeException($"Invalid IP mask length: {maskLength}.");

            Maskv6 = new ulong[]{ 0UL, 0UL };
            for (ushort i = 0; ip.AddressFamily == AddressFamily.InterNetworkV6 && i < maskLength; i++)
            {
                Maskv6[i / 64] = (Maskv6[i / 64] >> 1) | 0x8000_0000_0000_0000UL;
            }
        }
        else throw new ArgumentException($"Invalid IP address family: {ip.AddressFamily}.");

        IP = ip;
        MaskLength = maskLength;
    }
}
