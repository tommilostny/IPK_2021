using System;
using System.Net;
using System.Net.Sockets;

namespace ipk_l2l3_scan
{
    public class Subnet
    {
        public IPAddress Address { get; set; }
        public byte[] Mask { get; }
        public ushort MaskLength { get; }

        public Subnet(IPAddress ip, ushort maskLength)
        {
            Mask = ip.AddressFamily switch
            {
                AddressFamily.InterNetwork => CreateMask(maskLength, 32),
                AddressFamily.InterNetworkV6 => CreateMask(maskLength, 128),
                _ => throw new ArgumentException($"Invalid IP address family: {ip.AddressFamily}.")
            };
            Address = ApplyMask(ip, Mask);
            MaskLength = maskLength;
        }

        public bool IsAtMaxIpAddress(byte[] ipBytes = null)
        {
            if (ipBytes is null)
                ipBytes = Address.GetAddressBytes();

            bool allSet = true;
            for (int i = 0; i < ipBytes.Length && allSet; i++)
            {
                allSet = (ipBytes[i] | Mask[i]) == byte.MaxValue;
            }
            return allSet;
        }

        //Increment subnet IP Address if it isn't at max by mask
        public static Subnet operator ++(Subnet subnet)
        {
            var ipBytes = subnet.Address.GetAddressBytes();

            if (subnet.IsAtMaxIpAddress(ipBytes)) //do not increment 255.255.255.255
                return subnet;

            for (int i = ipBytes.Length - 1; i >= 0; i--)
            {
                if (++ipBytes[i] != 0) //no overflow, end cycle
                    break;
            }
            subnet.Address = new IPAddress(ipBytes);
            return subnet;
        }

        private static byte[] CreateMask(ushort maskLength, ushort maxMaskLength)
        {
            if (maskLength > maxMaskLength)
                throw new ArgumentOutOfRangeException($"Invalid IP mask length: {maskLength}.");

            //new byte array with length based on maxMaskLength parameter divided by size of byte (8) (right shift by 3)
            var mask = new byte[maxMaskLength >> 3];
            for (ushort i = 0; i < maskLength; i++)
            {
                int index = i / (maxMaskLength / mask.Length);
                mask[index] = (byte)(mask[index] >> 1 | 0x80);
            }
            return mask;
        }

        private static IPAddress ApplyMask(IPAddress address, byte[] mask)
        {
            var ipBytes = address.GetAddressBytes();
            for (int i = 0; i < ipBytes.Length; i++)
            {
                ipBytes[i] &= mask[i];
            }
            return new IPAddress(ipBytes);
        }
    }
}