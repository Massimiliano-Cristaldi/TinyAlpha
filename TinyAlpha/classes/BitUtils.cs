using System.Data;

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
}