using System;
using System.Runtime.InteropServices;

public struct IcmpPacket
{
    public ushort TypeCode;
    public ushort Checksum;
    public ushort Id;
    public ushort Sequence;

    private static ushort id = 0;
    private static ushort sequence = 0;
    public static byte[] EchoRequestAsBytes()
    {
        var packet = new IcmpPacket
        {
            TypeCode = 0x0008, //Type 8 - Echo request
            Id = ++id,
            Sequence = ++sequence
        };
        packet.Checksum = (ushort)(~((uint)packet.TypeCode + (uint)packet.Id + (uint)packet.Sequence));

        var size = Marshal.SizeOf(packet);
        var packetAsBytes = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(packet, ptr, true);
        Marshal.Copy(ptr, packetAsBytes, 0, size);
        Marshal.FreeHGlobal(ptr);

        return packetAsBytes;
    }
}
