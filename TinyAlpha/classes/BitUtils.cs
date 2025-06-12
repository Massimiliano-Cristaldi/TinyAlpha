using System.Buffers.Binary;

static class BitUtils
{
    public static List<bool> ByteToBits(byte inputByte)
    {
        List<bool> bits = [];

        for (int i = 0; i < 8; i++)
        {
            byte bitMask = (byte)(128 >> i);
            bool bitValue = (inputByte & bitMask) != 0;
            bits.Add(bitValue);
        }

        return bits;
    }

    public static byte BitsToByte(List<bool> inputBits)
    {
        byte bitCanvas = 0x00;
        for (int i = 0; i < inputBits.Count; i++)
        {
            byte bitMask = (byte)(inputBits[i] ? (128 >> i) : 0);
            bitCanvas |= bitMask;
        }
        return bitCanvas;
    }

    public static void BinDump(IEnumerable<byte> bytes)
    {
        string binaryDump = string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        System.Console.WriteLine(binaryDump);
    }

    public static void HexDump(IEnumerable<byte> bytes)
    {
        string hexDump = string.Join(" ", bytes.Select(h => Convert.ToString(h, 16).PadLeft(2, '0')));
        System.Console.WriteLine(hexDump);
    }
}