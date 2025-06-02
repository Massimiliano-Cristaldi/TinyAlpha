using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class TalEncoder
{
    Image<Rgba32> image;
    WBitStream head = new([]);
    WBitStream body = new([]);
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

    private IEnumerable<IGrouping<byte[], byte[]>> GetSortedColors()
    {
        List<byte[]> pixelBuf = [];

        image.ProcessPixelRows(accessor =>
        {
            for (int h = 0; h < accessor.Height; h++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(h);
                foreach (Rgba32 pixel in pixelRow)
                {
                    byte[] pixelBytes = [.. BitConverter.GetBytes(pixel.Rgba)];
                    pixelBuf.Add(pixelBytes);
                }
            }
        });

        if (pixelBuf.Count() > 256)
        {
            throw new TooManyColorsException("Encoding failed: source image has more than 256 colors.");
        }

        var sortedPixels = pixelBuf
                          .GroupBy(pixel => pixel)
                          .OrderByDescending(pixel => pixel.Count());

        return sortedPixels;
    }

    private void WriteLookupTable()
    {
        var sortedPixels = GetSortedColors();
        byte lookupTableLength = Convert.ToByte(sortedPixels.Count());
    }

    public void Encode(string outputPath)
    {
        WriteSignature();
        WriteWidthAndHeight();
        WriteLookupTable();

        // head.BinDump();
    }
}