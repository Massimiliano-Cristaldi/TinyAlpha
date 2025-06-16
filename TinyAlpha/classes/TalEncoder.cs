using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.RegularExpressions;

class TalEncoder
{
    Image<Rgba32> image;
    string inputFilename;

    WBitStream head = new();
    WBitStream chromaBitfield = new();
    WBitStream countBitfield = new();
    WBitStream colorTypeBitfield = new();
    WBitStream body = new();

    List<uint> sortedColors = [];
    uint[] favoriteColors = [];
    int streakCount = 0;
    uint currentColor;
    short streak;

    const string inputRootPath = "../Tests/png/";
    const string outputRootPath = "../Tests/tal/";

    public TalEncoder(string _inputFilename)
    {
        if (!File.Exists(inputRootPath + _inputFilename))
        {
            throw new FileNotFoundException($"File not found at path {inputRootPath + inputFilename}.");
        }

        image = Image.Load<Rgba32>(inputRootPath + _inputFilename);
        inputFilename = _inputFilename;
        currentColor = GetFirstColor();
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

        streakCount++;
        currentColor = color;
        streak = 1;
    }

    private void WriteSignatureAndVersion()
    {
        int[] magicNumber = [8, 9, 3, 1];
        byte[] signature = magicNumber.Select(n => (byte)n).ToArray();
        head.WriteBytes(signature);
    }

    private void WriteWidthAndHeight()
    {
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
                    colorScores[pixelRow[w].Rgba] = colorScores.GetValueOrDefault(pixelRow[w].Rgba) + 1;
                }
            }
        });

        if (colorScores.Count > 256)
        {
            throw new TooManyColorsException($"Failed encoding {inputFilename}: source image cannot have more than 256 colors.");
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
            lookUpTable.AddRange(BitConverter.GetBytes(color).Reverse().ToArray());
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
                    Rgba32 pixel = pixelRow[w];
                    if (pixel.Rgba == currentColor && streak < 256)
                    {
                        streak++;
                    }
                    else
                    {
                        ProcessStreak(pixel.Rgba);
                    }
                }
            }

            uint lastColor = GetLastColor();
            ProcessStreak(lastColor);
        });
    }

    private void WriteFile(string outputFilename)
    {
        List<byte> chromaBitfieldLength = BitConverter.GetBytes(chromaBitfield.Stream.Count).Reverse().ToList();
        List<byte> countBitfieldLength = BitConverter.GetBytes(countBitfield.Stream.Count).Reverse().ToList();
        List<byte> colorTypeBitfieldLength = BitConverter.GetBytes(colorTypeBitfield.Stream.Count).Reverse().ToList();
        List<byte> streakCountBytes = BitConverter.GetBytes(streakCount).Reverse().ToList();

        byte[] buffer = new[]
        {
            head.Stream,
            chromaBitfieldLength,
            countBitfieldLength,
            colorTypeBitfieldLength,
            streakCountBytes,
            chromaBitfield.Stream,
            countBitfield.Stream,
            colorTypeBitfield.Stream,
            body.Stream
        }.SelectMany(bytes => bytes)
         .ToArray();

        File.WriteAllBytes(outputRootPath + outputFilename, buffer);
    }

    public void Encode(bool overwriteIfExists)
    {
        Regex fileExt = new Regex(@"\..{2,}$");
        string outputFilename = fileExt.Replace(inputFilename, ".tal");

        if (File.Exists(inputRootPath + outputFilename) && !overwriteIfExists)
        {
            throw new FileAlreadyExistsException($"File name {outputFilename} is already taken. If you wish to overwrite it, pass the encode command a -o flag.");
        }

        WriteSignatureAndVersion();
        WriteWidthAndHeight();
        WriteLookupTable();
        WriteBody();

        WriteFile(outputFilename);

        System.Console.WriteLine($"File {outputFilename} was saved successfully.");
    }
}