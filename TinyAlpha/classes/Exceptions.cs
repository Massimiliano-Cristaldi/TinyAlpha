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