class TalCodec
{
    static void Main(string[] args)
    {
        // List<byte> data = [0b10110100, 0b10101100, 0b00110001, 0b11110001, 0b10110100, 0b10101100, 0b00110001, 0b11110001];

        // BitStream stream = new(data.GetRange(2, 5));
        // List<int> buf = stream.Read(34, true);

        BitStream stream = new([0]);
        stream.Write(false, 2);
        stream.Write(true, 3);
        stream.Write(false, 4);
        stream.Write(true, 5);
        stream.BinDump();
        stream.Write(false, 1);
        stream.Write(true, 1);
        stream.BinDump();
    }
}
