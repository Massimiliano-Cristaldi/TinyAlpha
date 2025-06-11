class TooManyColorsException : Exception
{
    public TooManyColorsException(string message) : base(message) { }
}

class FileAlreadyExistsException : Exception
{
    public FileAlreadyExistsException(string message) : base(message) { }
}

class UnrecognizedSignatureException : Exception
{
    public UnrecognizedSignatureException(string message) : base(message) { }
}

class VersionMismatchException : Exception
{
    public VersionMismatchException(string message) : base(message) { }
}

class ImageSizeException : Exception
{
    public ImageSizeException(string message) : base(message) { }
}