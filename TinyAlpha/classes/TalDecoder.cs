using System.Buffers.Binary;
using SixLabors.ImageSharp.PixelFormats;

class TalDecoder
{
    byte[] source;
    RBitStream lookupTable;
    RBitStream chromaBitfield;
    RBitStream countBitfield;
    RBitStream colorTypeBitfield;
    RBitStream body;
    uint[] tableValues;
    uint[] favoriteColors;
    int streakCount;
    int streakIndex = 0;
    List<uint> bitmap = [];
    const string inputRootPath = "../Tests/tal/";
    const string outputRootPath = "../Tests/png/";

    public TalDecoder(string inputFilename)
    {
        if (!File.Exists(inputRootPath + inputFilename))
        {
            throw new FileNotFoundException($"File not found at path {inputRootPath + inputFilename}.");
        }

        source = File.ReadAllBytes(inputRootPath + inputFilename);

        if (source.Length < 19)
        {
            throw new ImageSizeException("Unsupported image size. A .tal image cannot possibly be smaller than 19 bytes.");
        }
    }

    private void GetSegments()
    {
        int lookUpTableOffset = 13;
        int lookupTableLength = source[lookUpTableOffset - 1] * 4;
        int fieldLengthsOffset = lookUpTableOffset + lookupTableLength;

        int chromaBitfieldLength = BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(fieldLengthsOffset, 4));
        int chromaBitfieldOffset = fieldLengthsOffset + 16;

        int countBitfieldLength = BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(fieldLengthsOffset + 4, 4));
        int countBitfieldOffset = chromaBitfieldOffset + chromaBitfieldLength;

        int colorTypeBitfieldLength = BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(fieldLengthsOffset + 8, 4));
        int colorTypeBitfieldOffset = countBitfieldOffset + countBitfieldLength;

        streakCount = BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(fieldLengthsOffset + 12, 4));

        int bodyOffset = colorTypeBitfieldOffset + colorTypeBitfieldLength;
        int bodyLength = source.Length - bodyOffset;

        lookupTable = new(new ArraySegment<byte>(source, lookUpTableOffset, lookupTableLength));
        chromaBitfield = new(new ArraySegment<byte>(source, chromaBitfieldOffset, chromaBitfieldLength));
        countBitfield = new(new ArraySegment<byte>(source, countBitfieldOffset, countBitfieldLength));
        colorTypeBitfield = new(new ArraySegment<byte>(source, colorTypeBitfieldOffset, colorTypeBitfieldLength));
        body = new(new ArraySegment<byte>(source, bodyOffset, bodyLength));
    }

    private uint[] GetColors()
    {
        List<uint> colors = [];
        for (int i = 0; i < lookupTable.Stream.Count(); i += 4)
        {
            uint color = BinaryPrimitives.ReadUInt32BigEndian(lookupTable.Stream.AsSpan(i, 4));
            colors.Add(color);
        }
        return colors.ToArray();
    }

    private void ReadStream()
    {
        bool isStreak = countBitfield.ReadBit();
        // System.Console.WriteLine($"Is streak: {isStreak}");
        int bitsForIndex = chromaBitfield.ReadBit() ? (colorTypeBitfield.ReadBit() ? 4 : 8) : 0;
        // System.Console.WriteLine($"Bits for index: {bitsForIndex}");

        int count = isStreak ? BitUtils.BitsToByte(body.ReadBits(8)) : 1;
        int index = BitUtils.BitsToByte(body.ReadBits(bitsForIndex));
        // System.Console.WriteLine($"Index: {index}");

        uint colorValue = bitsForIndex > 0 ? tableValues[index] : 0;
        // System.Console.WriteLine($"Color value: {colorValue}");

        bitmap.AddRange(Enumerable.Repeat(colorValue, count));

        streakIndex++;
        if (streakIndex < streakCount)
        {
            ReadStream();
        }
    }

    public void Decode(string outputFilename, bool overwriteIfExists)
    {
        if (File.Exists(outputFilename) && !overwriteIfExists)
        {
            throw new FileAlreadyExistsException($"File name {outputFilename} is already taken. If you wish to overwrite it, pass the encode command a -o flag.");
        }

        ArraySegment<byte> signature = new(source, 0, 3);
        ArraySegment<byte> version = new(source, 3, 1);

        if (!signature.SequenceEqual(new byte[] { 0x08, 0x09, 0x03 }))
        {
            throw new UnrecognizedSignatureException("The decoder encountered an unexpected signature. Input file might be the wrong type or corrupted.");
        }

        if (!version.SequenceEqual(new byte[] { 0x01 }))
        {
            throw new VersionMismatchException("The decoder encountered an unexpected signature. Input file might have been decoded with a different encoder version.");
        }

        int width = BinaryPrimitives.ReadInt32BigEndian(new ArraySegment<byte>(source, 4, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(new ArraySegment<byte>(source, 8, 4));

        if (width > 8192 || height > 8192)
        {
            throw new ImageSizeException("Unsupported image size. Width and height must not exceed 8192px each.");
        }

        GetSegments();

        tableValues = GetColors();
        favoriteColors = tableValues.Take(16).ToArray();

        ReadStream();

        foreach (uint color in bitmap)
        {
            System.Console.WriteLine(color);
        }
    }    
}