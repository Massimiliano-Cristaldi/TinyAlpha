using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class TalDecoder
{
    byte[] source;
    RBitStream lookupTable;
    RBitStream chromaBitfield;
    RBitStream countBitfield;
    RBitStream colorTypeBitfield;
    RBitStream body;
    uint[] colors;
    uint[] favoriteColors;
    List<Rgba32> rgbaValues = [];
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
        int lookUpTableOffset = 15;
        int lookupTableLength = source[lookUpTableOffset - 1] * 4; // 8

        int chromaBitfieldLength = BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(lookUpTableOffset + lookupTableLength, 4));
        int chromaBitfieldOffset = lookUpTableOffset + lookupTableLength + 12;

        int countBitfieldLength = BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(lookUpTableOffset + lookupTableLength + 4, 4));
        int countBitfieldOffset = chromaBitfieldOffset + chromaBitfieldLength;

        int colorTypeBitfieldLength = BinaryPrimitives.ReadInt32BigEndian(source.AsSpan(lookUpTableOffset + lookupTableLength + 8, 4));
        int colorTypeBitfieldOffset = countBitfieldOffset + countBitfieldLength;

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
        if (countBitfield.ReadBit())
        {
            int count = BitUtils.BitsToByte(body.ReadBits(8));
        }
    }

    public void Decode(string outputFilename, bool overwriteIfExists)
    {
        if (File.Exists(outputFilename) && !overwriteIfExists)
        {
            throw new FileAlreadyExistsException($"File name {outputFilename} is already taken. If you wish to overwrite it, pass the encode command a -o flag.");
        }

        ArraySegment<byte> signature = new(source, 0, 3);
        ArraySegment<byte> version = new(source, 3, 3);

        if (!signature.SequenceEqual(new byte[] { 0x08, 0x09, 0x03 }))
        {
            throw new UnrecognizedSignatureException("The decoder encountered an unexpected signature. Input file might be the wrong type or corrupted.");
        }

        if (!version.SequenceEqual(new byte[] { 0x00, 0x00, 0x01 }))
        {
            throw new VersionMismatchException("The decoder encountered an unexpected signature. Input file might have been decoded with a different encoder version.");
        }

        int width = BinaryPrimitives.ReadInt32BigEndian(new ArraySegment<byte>(source, 6, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(new ArraySegment<byte>(source, 10, 4));

        if (width > 8192 || height > 8192)
        {
            throw new ImageSizeException("Unsupported image size. Width and height must not exceed 8192px each.");
        }

        GetSegments();

        colors = GetColors();
        favoriteColors = colors.Take(16).ToArray();

        ReadStream();
    }    
}