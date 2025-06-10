class TooManyColorsException : Exception
{
    public TooManyColorsException() { }
    public TooManyColorsException(string message) : base(message) { }
}

class FileAlreadyExistsException : Exception
{
    public FileAlreadyExistsException() { }
    public FileAlreadyExistsException(string message) : base(message) { }
}

class UnrecognizedSignatureException : Exception
{
    public UnrecognizedSignatureException() { }
    public UnrecognizedSignatureException(string message) : base(message) { }
}

class VersionMismatchException : Exception
{
    public VersionMismatchException() { }
    public VersionMismatchException(string message) : base(message) { }
}