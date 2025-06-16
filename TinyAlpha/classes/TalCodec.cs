class TalCodec
{
    static void Main(string[] args)
    {
        int argCount = args.Length;

        if (argCount < 2 || argCount > 3)
        {
            throw new ArgumentException($"TalCodec requires 2-3 arguments: encode/decode, an input filename and a -o flag (optional). {argCount} arguments provided.");
        }

        if (args[1].Any(c => Path.GetInvalidPathChars().Contains(c)))
            {
                throw new InvalidPathException($"\"{args[1]}\" is not a valid file path.");
            }

        if (argCount == 3 && args[2] != "-o")
        {
            throw new ArgumentException($"Invalid argument \"{args[2]}\": args[2] must be \"-o\" if present.");
        }

        bool overwriteIfExists = argCount == 3 && args[2] == "-o";

        if (args[0] == "encode")
        {
            TalEncoder encoder = new(args[1]);
            encoder.Encode(overwriteIfExists);
        }
        else if (args[0] == "decode")
        {
            TalDecoder decoder = new(args[1]);
            decoder.Decode(overwriteIfExists);
        }
        else
        {
            throw new ArgumentException($"Invalid argument \"{args[0]}\": args[0] must be either \"encode\" or \"decode\".");
        }

    }
}