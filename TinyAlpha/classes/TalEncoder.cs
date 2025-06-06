using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class TalEncoder(string inputPath)
{
    Image<Rgba32> image = Image.Load<Rgba32>(imageTestRoot + inputPath);
    WBitStream head = new([]);
    WBitStream chromaBitfield = new([]);
    WBitStream countBitfield = new([]);
    WBitStream colorTypeBitfield = new([]);
    WBitStream body = new([]);
    List<uint> sortedColors = [];

    const string imageTestRoot = "../Tests/images/";

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

        byte lookupTableLength = Convert.ToByte(sortedColors.Count);
        List<byte> lookUpTable = [lookupTableLength];

        foreach (uint color in sortedColors)
        {
            lookUpTable = [.. lookUpTable, .. BitConverter.GetBytes(color)];
        }

        head.WriteBytes(lookUpTable.ToArray());
    }

    private void WriteBody()
    {
        List<uint> favoriteColors = sortedColors.Take(16).ToList();

        image.ProcessPixelRows(accessor =>
        {
            short streak = 1;

            for (int h = 0; h < accessor.Height; h++)
            {

                Span<Rgba32> pixelRow = accessor.GetRowSpan(h);
                uint currentColor = pixelRow[0].Rgba;

                for (int w = 1; w < accessor.Width; w++)
                {
                    bool isLastPixelInRow = w == accessor.Width - 1;

                    if (pixelRow[w].Rgba == currentColor && !isLastPixelInRow)
                    {
                        streak++;
                        continue;
                    }
                    else if (pixelRow[w].Rgba != currentColor && currentColor > 0x00)
                    {
                        chromaBitfield.WriteBit(true);
                        countBitfield.WriteBit(streak > 1);
                        colorTypeBitfield.WriteBit(favoriteColors.Contains(currentColor));
                        if (streak > 1)
                        {
                            body.WriteBytes(BitConverter.GetBytes(streak));
                        }
                        body.WriteBytes(BitConverter.GetBytes(currentColor));
                    }
                    else
                    {
                        chromaBitfield.WriteBit(false);
                        countBitfield.WriteBit(streak > 1);
                        if (streak > 1)
                        {
                            body.WriteBytes(BitConverter.GetBytes(streak));
                        }
                    }

                    currentColor = pixelRow[w].Rgba;
                    streak = 1;
                }
            }
        });
    }

    public void Encode(string inputPath)
    {
        WriteSignature();
        WriteWidthAndHeight();
        WriteLookupTable();
        WriteBody();

        System.Console.WriteLine("##### HEAD #####");
        head.HexDump();
        System.Console.WriteLine("##### CHROMA BITFIELD #####");
        chromaBitfield.HexDump();
        System.Console.WriteLine("##### COUNT BITFIELD #####");
        countBitfield.HexDump();
        System.Console.WriteLine("##### COLOR TYPE BITFIELD #####");
        colorTypeBitfield.HexDump();
        System.Console.WriteLine("##### BODY #####");
        body.HexDump();
    }
}