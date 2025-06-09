abstract class BitStream
{
    public List<byte> stream = [];
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
    public RBitStream(List<byte> binData)
    {
        ArgumentNullException.ThrowIfNull(binData);

        stream = binData;
        ByteIndex = 0;
    }

    public bool ReadBit()
    {
        int bitReadMask = 128 >> BitIndex;
        bool bitValue = (stream[ByteIndex] & bitReadMask) != 0;
        BitIndex++;
        
        return bitValue;
    }

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
}

class WBitStream : BitStream
{
    public void WriteBit(bool bitValue)
    {
        if (ByteIndex > stream.Count - 1)
        {
            int bitWriteMask = bitValue ? 128 : 0;
            stream.Add((byte)bitWriteMask);
        }
        else if (bitValue)
        {
            stream[ByteIndex] |= (byte)(128 >> BitIndex);
        }
        BitIndex++;
    }

    public void WriteBits(List<bool> bits)
    {
        foreach (bool bit in bits)
        {
            if (ByteIndex > stream.Count - 1)
            {
                int bitWriteMask = bit ? 128 : 0;
                stream.Add((byte)bitWriteMask);
            }
            else if (bit)
            {
                stream[ByteIndex] |= (byte)(128 >> BitIndex);
            }
            BitIndex++;
        }
    }

    public void WriteByte(byte byteValue)
    {
        stream.Add(byteValue);
        BitIndex += 8;
    }

    public void WriteBytes(byte[] bytes)
    {
        stream.AddRange(bytes);
        BitIndex += (bytes.Length - 1) * 8 + (8 - BitIndex);
    }
}