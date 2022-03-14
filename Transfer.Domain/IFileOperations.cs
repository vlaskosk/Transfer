
namespace Transfer.Domain
{
    public interface IFileOperations
    {
        void CopyFile(CopyTaskDetails copyTaskDetails);
        List<CopyTaskDetails> GetTasksFromDirectory(string source, string destination);
    }
}