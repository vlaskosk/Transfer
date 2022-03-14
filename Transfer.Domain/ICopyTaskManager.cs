
namespace Transfer.Domain
{
    public interface ICopyTaskManager
    {
        void AddDirectories(string source, string destination);
        Task End();
        List<CopyTaskDetails> GetStatus();
        Task Start();
    }
}