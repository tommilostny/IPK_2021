using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ipk_l2l3_scan
{
    public struct IcmpPacket
    {
        public ushort TypeCode;
        public ushort Checksum;
        public ushort Id;
        public ushort Sequence;

        private static ushort id = 0;
        private static ushort sequence = 0;
        public static byte[] EchoRequestAsBytes(ProtocolType protocol)
        {
            var packet = new IcmpPacket
            {
                //Echo request types:
                //ICMPv4: Type 8
                //ICMPv6: Type 128
                TypeCode = protocol == ProtocolType.Icmp ? (ushort)0x0008 : (ushort)0x0080,
                Id = ++id,
                Sequence = ++sequence
            };
            packet.Checksum = (ushort)~(packet.TypeCode + (uint)packet.Id + packet.Sequence);

            var size = Marshal.SizeOf(packet);
            var packetAsBytes = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(packet, ptr, true);
            Marshal.Copy(ptr, packetAsBytes, 0, size);
            Marshal.FreeHGlobal(ptr);

            return packetAsBytes;
        }
    }
}