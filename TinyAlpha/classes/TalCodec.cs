class TalCodec
{
    static void Main(string[] args)
    {
        bool overwriteIfExists = args.Contains("-o");

        // TalEncoder encoder = new("8x8bwa.png");
        // encoder.Encode("8x8bwa.tal", true);

        TalDecoder decoder = new("8x8bwa.tal");
        decoder.Decode("8x8bwa.png", true);
    }
}