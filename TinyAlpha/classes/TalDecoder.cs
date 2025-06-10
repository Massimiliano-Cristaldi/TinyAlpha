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
        source = new(sourceBytes);
    }

    public (int, int, int, int, int) GetOffsets()
    {
        int lookUpTableOffset = 14;
        int lookupTableLength = source.stream[lookUpTableOffset];

        int chromaBitfieldLength = source.stream[lookUpTableOffset + lookupTableLength];
        int chromaBitfieldOffset = source.stream[lookUpTableOffset + lookupTableLength + 2];

        int countBitfieldLength = source.stream[lookUpTableOffset + lookupTableLength + 1];
        int countBitfieldOffset = source.stream[chromaBitfieldOffset + chromaBitfieldLength];

        int colorTypeBitfieldLength = source.stream[lookUpTableOffset + lookupTableLength + 2];
        int colorTypeBitfieldOffset = source.stream[countBitfieldOffset + countBitfieldLength];

        int bodyOffset = source.stream[colorTypeBitfieldOffset + colorTypeBitfieldLength];

        return (lookUpTableOffset, chromaBitfieldOffset, countBitfieldOffset, colorTypeBitfieldOffset, bodyOffset);
    }

    public void Decode(string outputFilename, bool overwriteIfExists)
    {
        if (File.Exists(outputFilename) && !overwriteIfExists)
        {
            throw new FileAlreadyExistsException($"File name {outputFilename} is already taken. If you wish to overwrite it, pass the encode command a -o flag.");
        }

        (
            int lookUpTableOffset,
            int chromaBitfieldOffset,
            int countBitfieldOffset,
            int colorTypeBitfieldOffset,
            int bodyffset
        ) = GetOffsets();

        Span<byte> signature = new(source.stream, 0, 2);
        Span<byte> version = new(source.stream, 3, 5);

        if (signature != BitConverter.GetBytes(0x080903))
        {
            throw new UnrecognizedSignatureException("Decoder encountered an unexpected signature. Input file might be the wrong type or corrupted.");
        }

        if (version != BitConverter.GetBytes(0x000001))
        {
            throw new VersionMismatchException("Decoder encountered an unexpected signature. Input file might have been decoded with a different encoder version.");
        }
    }    
}