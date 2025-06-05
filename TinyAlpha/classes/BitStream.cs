abstract class BitStream
{
    public List<byte> stream;
    private int bitIndex = 0;
    public int BitIndex
    {
        get { return bitIndex; }
        set
        {
            ByteIndex += value / 8;
            bitIndex = value % 8;
        }
    }

    protected int ByteIndex { get; set; }

    public BitStream(List<byte> binData)
    {
        ArgumentNullException.ThrowIfNull(binData);

        stream = binData;
        ByteIndex = 0;
    }

    public void BinDump()
    {
        string binaryDump = string.Join(" ", stream.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        System.Console.WriteLine(binaryDump);
        CoordsDump();
    }

    public void HexDump()
    {
        string hexDump = string.Join(" ", stream.Select(h => Convert.ToString(h, 16).PadLeft(2, '0')));
        System.Console.WriteLine(hexDump);
        CoordsDump();
    }

    public void CoordsDump()
    {
        System.Console.WriteLine($"Pointer at byte {ByteIndex} - bit {BitIndex}");
    }
}

class RBitStream : BitStream
{
    public RBitStream(List<byte> binData) : base(binData) { }

    public List<bool> ReadBits(int count)
    {
        List<bool> buf = [];

        for (int i = 0; i < count; i++)
        {
            int bitReadMask = 128 >> BitIndex;
            bool bitValue = (stream[ByteIndex] & bitReadMask) != 0;
            buf.Add(bitValue);
            BitIndex++;
        }

        return buf;
    }

    public static byte BitsToByte(List<bool> bits)
    {
        byte bitCanvas = 0x00;
        for (int i = 0; i < bits.Count; i++)
        {
            byte bitMask = Convert.ToByte(bits[i] ? (128 >> i) : 0);
            bitCanvas |= bitMask;
        }
        return bitCanvas;
    }
}

class WBitStream : BitStream
{
    public WBitStream(List<byte> binData) : base(binData) { }

    public void WriteBit(bool bitValue)
    {
        if (ByteIndex > stream.Count - 1)
        {
            int bitWriteMask = bitValue ? 128 : 0;
            stream.Add(Convert.ToByte(bitWriteMask));
        }
        else if (bitValue)
        {
            stream[ByteIndex] |= (byte)(128 >> BitIndex);
        }
        BitIndex++;        
    }

    public void WriteBits(bool bitValue, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (ByteIndex > stream.Count - 1)
            {
                int bitWriteMask = bitValue ? 128 : 0;
                stream.Add(Convert.ToByte(bitWriteMask));
            }
            else if (bitValue)
            {
                stream[ByteIndex] |= (byte)(128 >> BitIndex);
            }
            BitIndex++;
        }
    }

    public void WriteBytes(List<byte> bytes)
    {
        stream = [.. stream, .. bytes];
        BitIndex += (bytes.Count - 1) * 8 + (8 - BitIndex);
    }
}