class TalCodec
{
    static void Main(string[] args)
    {
        TalEncoder encoder = new("8x8bwa.png");
        encoder.Encode("8x8bwa.tal");
    }
}