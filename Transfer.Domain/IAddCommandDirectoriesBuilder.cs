namespace Transfer.Domain
{
    public interface IAddCommandDirectoriesBuilder
    {
        (string sourceDir, string destinationDir, bool okCommand) BuildDirectories(string command);
    }
}