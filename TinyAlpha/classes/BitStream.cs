using System.IO;
using System.Runtime.InteropServices;

class BitStream
{
    public List<byte> stream;
    private int bitIndex = 0;

    public BitStream(List<byte> binData)
    {
        ArgumentNullException.ThrowIfNull(binData);

        stream = binData;
        ByteIndex = 0;
    }

    public int BitIndex
    {
        get { return bitIndex; }
        set
        {
            ByteIndex += value > 7 ? 1 : 0;
            bitIndex = value % 8;
        }
    }

    private int ByteIndex { get; set; }

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

    public List<bool> Read(int count, bool debug = false)
    {
        List<bool> buf = [];

        if (debug)
        {
            System.Console.WriteLine($"*** Byte # {ByteIndex} ***");
        }

        for (int i = 0; i < count; i++)
        {
            int bitReadMask = 128 >> BitIndex;
            bool bitValue = (stream[ByteIndex] & bitReadMask) != 0;
            buf.Add(bitValue);
            BitIndex++;

            if (debug)
            {
                System.Console.Write(bitValue);
                if ((i + 1) % 8 == 0 && i != 0)
                {
                    System.Console.Write("\n");
                    System.Console.WriteLine($"*** Byte # {ByteIndex} ***");
                }
            }
        }

        return buf;
    }

    public void Write(bool bitValue, int count)
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
}