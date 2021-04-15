using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

public class Subnet
{
    public IPAddress Address { get; private set; }

    public byte[] Mask { get; }

    public ushort Length { get; }

    public Subnet(IPAddress ip, ushort maskLength)
    {
        Mask = ip.AddressFamily switch
        {
            AddressFamily.InterNetwork => CreateMask(maskLength, 32),
            AddressFamily.InterNetworkV6 => CreateMask(maskLength, 128),
            _ => throw new ArgumentException($"Invalid IP address family: {ip.AddressFamily}.")
        };

        Address = ApplyMask(ip, Mask);

        Length = ip.AddressFamily switch
        {
            AddressFamily.InterNetwork => (ushort)(32 - maskLength),
            _ => (ushort)(128 - maskLength)
        };
    }

    public IPAddress IncrementIP()
    {
        var ipBytes = Address.GetAddressBytes();

        if (ipBytes.All(b => b == byte.MaxValue)) //do not increment 255.255.255.255
            return Address;

        for (int i = ipBytes.Length - 1; i >= 0; i--)
        {
            if (++ipBytes[i] != 0) //no overflow, end cycle
                break;
        }
        return (Address = new IPAddress(ipBytes));
    }

    private byte[] CreateMask(ushort maskLength, ushort maxMaskLength)
    {
        if (maskLength > maxMaskLength)
            throw new ArgumentOutOfRangeException($"Invalid IP mask length: {maskLength}.");

        var mask = new byte[maxMaskLength >> 3];
        for (ushort i = 0; i < maskLength; i++)
        {
            var index = i / (maxMaskLength >> (int)Math.Sqrt(mask.Length));
            mask[index] = (byte)((mask[index] >> 1) | 0x80);
        }
        return mask;
    }

    private IPAddress ApplyMask(IPAddress address, byte[] mask)
    {
        var ipBytes = address.GetAddressBytes();
        for (int i = 0; i < ipBytes.Length; i++)
        {
            ipBytes[i] &= mask[i];
        }
        return new IPAddress(ipBytes);
    }
}
