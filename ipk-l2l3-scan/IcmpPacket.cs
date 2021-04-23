using System;
using System.Runtime.InteropServices;

public struct IcmpPacket
{
    public byte Type;
    public byte Code;
    public ushort Checksum;
    public ushort Id;
    public ushort Sequence;

    private static ushort id = 0;
    private static ushort sequence = 0;
    public static byte[] RequestAsBytes()
    {
        var packet = new IcmpPacket
        {
            Type = 8,
            Code = 0,
            Checksum = 0,
            Id = ++id,
            Sequence = ++sequence
        };
        var size = Marshal.SizeOf(packet);
        var packetAsBytes = new byte[size + 32];

        IntPtr ptr = Marshal.AllocHGlobal(size + 32);
        Marshal.StructureToPtr(packet, ptr, true);
        Marshal.Copy(ptr, packetAsBytes, 0, size);
        Marshal.FreeHGlobal(ptr);

        return packetAsBytes;
    }
}
