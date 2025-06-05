using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class TalEncoder
{
    Image<Rgba32> image;
    WBitStream head = new([]);
    WBitStream chromaBitfield = new([]);
    WBitStream countBitfield = new([]);
    WBitStream colorTypeBitfield = new([]);
    const string imageTestRoot = "../Tests/images/";

    public TalEncoder(string inputPath)
    {
        image = Image.Load<Rgba32>(imageTestRoot + inputPath);
    }
    private void WriteSignature()
    {
        int[] magicNumber = [8, 9, 3, 0, 0, 1];
        List<byte> signature = [.. magicNumber.Select(n => (byte)n)];
        head.WriteBytes(signature);
    }

    private void WriteWidthAndHeight()
    {
        // Reverse converts to Big Endian before writing
        List<byte> widthBytes = [.. BitConverter.GetBytes(image.Width).Reverse()];
        List<byte> heightBytes = [.. BitConverter.GetBytes(image.Height).Reverse()];
        head.WriteBytes(widthBytes);
        head.WriteBytes(heightBytes);
    }

    private void WriteLookupTable()
    {
        IEnumerable<KeyValuePair<uint, int>> sortedColors;
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

        // Transparent pixels are not written in the lookup table
        sortedColors = colorScores.OrderByDescending(color => color.Value).Where(color => color.Key != 0);

        byte lookupTableLength = Convert.ToByte(sortedColors.Count());
        List<byte> lookUpTable = [lookupTableLength];

        foreach (KeyValuePair<uint, int> color in sortedColors)
        {
            lookUpTable = [.. lookUpTable, .. BitConverter.GetBytes(color.Key)];
        }

        head.WriteBytes(lookUpTable);
    }

    private void WriteBody()
    {
        image.ProcessPixelRows(accessor =>
        {
            uint currentColor = 0;
            Int16 streak = 0;

            for (int h = 0; h < accessor.Height; h++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(h);
                for (int w = 0; w < accessor.Width; w++)
                {
                    if (pixelRow[w].Rgba == currentColor)
                    {
                        streak++;
                    }
                    else
                    {
                        currentColor = pixelRow[w].Rgba;
                        streak = 1;
                    }

                    chromaBitfield.WriteBit(pixelRow[w].A > 0xf0);
                }
            }
        });
    }

    public void Encode(string outputPath)
    {
        WriteSignature();
        WriteWidthAndHeight();
        WriteLookupTable();

        head.HexDump();
    }
}