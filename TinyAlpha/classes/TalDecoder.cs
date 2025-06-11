using System.Buffers.Binary;
using SixLabors.ImageSharp;

class TalDecoder
{
    RBitStream source;
    const string inputRootPath = "../Tests/input/";
    const string outputRootPath = "../Tests/output/";

    public TalDecoder(string inputFilename)
    {
        if (!File.Exists(inputRootPath + inputFilename))
        {
            throw new FileNotFoundException($"File not found at path {inputRootPath + inputFilename}.");
        }

        byte[] sourceBytes = File.ReadAllBytes(inputRootPath + inputFilename);

        if (sourceBytes.Length < 19)
        {
            throw new ImageSizeException("Unsupported image size. A .tal image cannot possibly be smaller than 19 bytes.");
        }

        source = new(sourceBytes);
    }

    public (ArraySegment<byte>, ArraySegment<byte>, ArraySegment<byte>, ArraySegment<byte>, ArraySegment<byte>) GetSegments()
    {
        int lookUpTableOffset = 14;
        int lookupTableLength = source.Stream[lookUpTableOffset];

        int chromaBitfieldLength = source.Stream[lookUpTableOffset + lookupTableLength];
        int chromaBitfieldOffset = lookUpTableOffset + lookupTableLength + 3;

        int countBitfieldLength = source.Stream[lookUpTableOffset + lookupTableLength + 1];
        int countBitfieldOffset = chromaBitfieldOffset + chromaBitfieldLength;

        int colorTypeBitfieldLength = source.Stream[lookUpTableOffset + lookupTableLength + 2];
        int colorTypeBitfieldOffset = countBitfieldOffset + countBitfieldLength;

        int bodyOffset = colorTypeBitfieldOffset + colorTypeBitfieldLength;
        int bodyLength = source.Stream.Length - bodyOffset - 1;

        ArraySegment<byte> lookupTable = new(source.Stream, lookUpTableOffset, lookupTableLength);
        ArraySegment<byte> chromaBitfield = new(source.Stream, chromaBitfieldOffset, chromaBitfieldLength);
        ArraySegment<byte> countBitfield = new(source.Stream, countBitfieldOffset, countBitfieldLength);
        ArraySegment<byte> colorTypeBitfield = new(source.Stream, colorTypeBitfieldOffset, colorTypeBitfieldLength);
        ArraySegment<byte> body = new(source.Stream, bodyOffset, bodyLength);

        return (lookupTable, chromaBitfield, countBitfield, colorTypeBitfield, body);
    }

    public uint[] GetColors(byte[] lookupTable)
    {
        List<uint> colors = [];
        for (int i = 0; i < lookupTable.Length; i += 4)
        {
            colors.Add(BinaryPrimitives.ReadUInt32LittleEndian(lookupTable.AsSpan(i, 4)));
        }
        return colors.ToArray();
    }

    public void Decode(string outputFilename, bool overwriteIfExists)
    {
        if (File.Exists(outputFilename) && !overwriteIfExists)
        {
            throw new FileAlreadyExistsException($"File name {outputFilename} is already taken. If you wish to overwrite it, pass the encode command a -o flag.");
        }

        ArraySegment<byte> signature = new(source.Stream, 0, 3);
        ArraySegment<byte> version = new(source.Stream, 3, 3);

        ArraySegment<byte> width = new(source.Stream, 6, 4);
        ArraySegment<byte> height = new(source.Stream, 10, 4);

        if (!signature.SequenceEqual(new byte[] { 0x08, 0x09, 0x03 }))
        {
            throw new UnrecognizedSignatureException("The decoder encountered an unexpected signature. Input file might be the wrong type or corrupted.");
        }

        if (!version.SequenceEqual(new byte[] { 0x00, 0x00, 0x01 }))
        {
            throw new VersionMismatchException("The decoder encountered an unexpected signature. Input file might have been decoded with a different encoder version.");
        }

        if (BinaryPrimitives.ReadInt32BigEndian(width) > 8192 || BinaryPrimitives.ReadInt32BigEndian(height) > 8192)
        {
            throw new ImageSizeException("Unsupported image size. Width and height must not exceed 8192px each.");
        }

        (
            ArraySegment<byte> lookUpTable,
            ArraySegment<byte> chromaBitfield,
            ArraySegment<byte> countBitfield,
            ArraySegment<byte> colorTypeBitfield,
            ArraySegment<byte> body
        ) = GetSegments();

        uint[] colors = GetColors(lookUpTable.ToArray());
        foreach (uint color in colors)
        {
            System.Console.WriteLine(color);
        }
    }    
}