abstract class BitStream
{
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

    public void PrintCoords()
    {
        System.Console.WriteLine($"Pointer at byte {ByteIndex} - bit {BitIndex}");
    }
}

class RBitStream : BitStream
{
    public byte[] Stream
    {
        get;
        private set;
    }

    public RBitStream(byte[] binData)
    {
        ArgumentNullException.ThrowIfNull(binData);

        Stream = binData;
        ByteIndex = 0;
    }

    public bool ReadBit()
    {
        int bitReadMask = 128 >> BitIndex;
        bool bitValue = (Stream[ByteIndex] & bitReadMask) != 0;
        BitIndex++;

        return bitValue;
    }

    public List<bool> ReadBits(int count)
    {
        List<bool> buf = [];

        for (int i = 0; i < count; i++)
        {
            int bitReadMask = 128 >> BitIndex;
            bool bitValue = (Stream[ByteIndex] & bitReadMask) != 0;
            buf.Add(bitValue);
            BitIndex++;
        }

        return buf;
    }
}

class WBitStream : BitStream
{
    public List<byte> Stream
    {
        get;
        private set;
    }

    public WBitStream()
    {
        Stream = [];
    }

    public void WriteBit(bool bitValue)
    {
        if (ByteIndex > Stream.Count - 1)
        {
            int bitWriteMask = bitValue ? 128 : 0;
            Stream.Add((byte)bitWriteMask);
        }
        else if (bitValue)
        {
            Stream[ByteIndex] |= (byte)(128 >> BitIndex);
        }
        BitIndex++;
    }

    public void WriteBits(List<bool> bits)
    {
        foreach (bool bit in bits)
        {
            if (ByteIndex > Stream.Count - 1)
            {
                int bitWriteMask = bit ? 128 : 0;
                Stream.Add((byte)bitWriteMask);
            }
            else if (bit)
            {
                Stream[ByteIndex] |= (byte)(128 >> BitIndex);
            }
            BitIndex++;
        }
    }

    public void WriteByte(byte byteValue)
    {
        Stream.Add(byteValue);
        BitIndex += 8;
    }

    public void WriteBytes(byte[] bytes)
    {
        Stream.AddRange(bytes);
        BitIndex += (bytes.Length - 1) * 8 + (8 - BitIndex);
    }
}