using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class TalEncoder
{
    Image<Rgba32> image;
    string inputPath;

    WBitStream head = new([]);
    WBitStream chromaBitfield = new([]);
    WBitStream countBitfield = new([]);
    WBitStream colorTypeBitfield = new([]);
    WBitStream body = new([]);

    List<uint> sortedColors = [];
    uint[] favoriteColors = [];
    uint currentColor;
    short streak;

    const string imageTestRootPath = "../Tests/images/";

    public TalEncoder(string _inputPath)
    {
        image = Image.Load<Rgba32>(imageTestRootPath + _inputPath);
        inputPath = _inputPath;
        currentColor = GetFirstColor();
    }

    private void WriteSignature()
    {
        int[] magicNumber = [8, 9, 3, 0, 0, 1];
        byte[] signature = magicNumber.Select(n => (byte)n).ToArray();
        head.WriteBytes(signature);
    }

    private void WriteWidthAndHeight()
    {
        // Reverse converts to Big Endian before writing
        byte[] widthBytes = BitConverter.GetBytes(image.Width).Reverse().ToArray();
        byte[] heightBytes = BitConverter.GetBytes(image.Height).Reverse().ToArray();
        head.WriteBytes(widthBytes);
        head.WriteBytes(heightBytes);
    }

    private void WriteLookupTable()
    {
        Dictionary<uint, int> colorScores = [];

        image.ProcessPixelRows(accessor =>
        {
            for (int h = 0; h < accessor.Height; h++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(h);
                for (int w = 0; w < accessor.Width; w++)
                {
                    // TODO: handle semitransparent pixels
                    colorScores[pixelRow[w].Rgba] = colorScores.GetValueOrDefault(pixelRow[w].Rgba) + 1;
                }
            }
        });

        if (colorScores.Count > 256)
        {
            throw new TooManyColorsException($"Failed encoding {inputPath}: source image cannot have more than 256 colors.");
        }

        sortedColors = colorScores
                       .Where(color => color.Key != 0)
                       .OrderByDescending(color => color.Value)
                       .Select(color => color.Key)
                       .ToList();

        byte lookupTableLength = (byte)sortedColors.Count;
        List<byte> lookUpTable = [lookupTableLength];

        foreach (uint color in sortedColors)
        {
            lookUpTable.AddRange(BitConverter.GetBytes(color));
        }

        head.WriteBytes(lookUpTable.ToArray());
    }

    private void WriteBody()
    {
        favoriteColors = sortedColors.Take(16).ToArray();

        image.ProcessPixelRows(accessor =>
        {
            for (int h = 0; h < accessor.Height; h++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(h);

                for (int w = 0; w < accessor.Width; w++)
                {
                    if (pixelRow[w].Rgba == currentColor && streak < 256)
                    {
                        streak++;
                    }
                    else
                    {
                        ProcessStreak(pixelRow[w].Rgba);
                    }
                }
            }

            uint lastColor = GetLastColor();
            ProcessStreak(lastColor);
        });
    }

    private uint GetFirstColor()
    {
        uint firstColor = 0;

        image.ProcessPixelRows(accessor =>
        {
            Rgba32 firstPixel = accessor.GetRowSpan(0)[0];
            firstColor = firstPixel.Rgba;
        });

        return firstColor;
    }

    private uint GetLastColor()
    {
        uint lastColor = 0;

        image.ProcessPixelRows(accessor =>
        {
            int maxH = accessor.Height - 1;
            int maxW = accessor.Width - 1;
            Rgba32 lastPixel = accessor.GetRowSpan(maxH)[maxW];
            lastColor = lastPixel.Rgba;
        });

        return lastColor;
    }

    private List<bool> GetColorIndexBits(uint targetColor)
    {
        List<bool> bits = [];

        byte index = (byte)Array.FindIndex(favoriteColors, favColor => favColor == targetColor);
        int offset = index < 16 ? 4 : 0;
        for (int i = 0; i < (8 - offset); i++)
        {
            byte bitMask = (byte)(128 >> (i + offset));
            bool bitValue = (index & bitMask) != 0;
            bits.Add(bitValue);
        }

        return bits;
    }

    public void ProcessStreak(uint color)
    {
        chromaBitfield.WriteBit(currentColor > 0);
        countBitfield.WriteBit(streak > 1);

        if (streak > 1)
        {
            List<bool> streakBits = BitUtils.ByteToBits((byte)streak);
            body.WriteBits(streakBits);
        }

        if (currentColor > 0)
        {
            colorTypeBitfield.WriteBit(favoriteColors.Contains(currentColor));
            List<bool> colorIndex = GetColorIndexBits(currentColor);
            body.WriteBits(colorIndex);
        }

        currentColor = color;
        streak = 1;
    }

    public void Encode(string inputPath)
    {
        WriteSignature();
        WriteWidthAndHeight();
        WriteLookupTable();
        WriteBody();

        System.Console.WriteLine("##### HEAD #####");
        head.BinDump();
        System.Console.WriteLine("##### CHROMA BITFIELD #####");
        chromaBitfield.BinDump();
        // Expected: 11010111 10101111
        // Got: 11010111 10101111
        System.Console.WriteLine("##### COUNT BITFIELD #####");
        countBitfield.BinDump();
        // Expected: 11101101 11010011
        // Got: 11101101 11010011
        System.Console.WriteLine("##### COLOR TYPE BITFIELD #####");
        colorTypeBitfield.BinDump();
        // Expected: 11111111 11110000
        // Got: 11111111 11110000
        System.Console.WriteLine("##### BODY #####");
        body.BinDump();
        // Expected: 00000111 0001 00001001 0000 00000101 0000 00000010 00000100 0001 0000 00000011 0001 00001000 0000 00000010 0000 00000101 0001 0000 00000110 0001 00001000 0000
        // Got:      00000111 0001 00001001 0000 00000101 0000 00000010 00000100 0001 0000 00000011 0001 00001000 0000 00000010 0000 00000101 0001 0000 00000110 0001 00001000 0000
    }
}