namespace Transfer.Domain
{
    public interface IPersistCopyTasks
    {
        Task Init();

        Task RemoveCopyTask(CopyTaskDetails taskToRemove);

        Task AddCopyTask(CopyTaskDetails addTask);

        List<CopyTaskDetails> GetIncompleteCopyTasks();
    }
}
