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

    private void CyclePixels()
    {
        Dictionary<uint, int> sortedColors = [];

        image.ProcessPixelRows(accessor =>
        {
            for (int h = 0; h < accessor.Height; h++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(h);
                for (int w = 0; w < accessor.Width; w++)
                {
                    // Count colors for lookup table order
                    sortedColors[pixelRow[w].Rgba] = sortedColors.GetValueOrDefault(pixelRow[w].Rgba) + 1;
                    // Keep track of count for count bitfield
                    int nextIndex = Math.Min(w + 1, accessor.Width - 1);
                    uint nextColor = pixelRow[nextIndex].Rgba;
                    // Check if pixel is colored or transparent -> Only has to be done if it's a single pixel or the end of a count sequence
                    chromaBitfield.WriteBits(pixelRow[w].A > 0xf0, 1);
                    // Check if it's a favorite color -> Has to be done later, because we need to iterate all the pixels before we can sort the colors
                }
            }
        });

    }

    public void Encode(string outputPath)
    {
        WriteSignature();
        WriteWidthAndHeight();

        CyclePixels();

        head.HexDump();
    }
}