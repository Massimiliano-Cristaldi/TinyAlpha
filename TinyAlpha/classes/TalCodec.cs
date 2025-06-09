class TalCodec
{
    static void Main(string[] args)
    {
        bool overwriteIfExists = args.Contains("-o");
        System.Console.WriteLine(overwriteIfExists);

        TalEncoder encoder = new("8x8bwa.png");
        encoder.Encode("../Tests/output/8x8bwa", overwriteIfExists);
    }
}