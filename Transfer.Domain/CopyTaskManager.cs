using System.Collections.Concurrent;

namespace Transfer.Domain
{
    public class CopyTaskManager : ICopyTaskManager
    {
        private const int WaitTimeInSeconds = 1;

        private readonly ConcurrentBag<CopyTaskDetails> _allTaskDetails;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CopyTaskDetails>> _tasksByExtension;

        private readonly AutoResetEvent _canProcess;
        private readonly Dictionary<string, SemaphoreSlim> _extensionLocks;

        private readonly IPersistCopyTasks _persistCopyTasks;
        private readonly IFileOperations _fileOperations;
        private Task _manageCompletionTask;
        private bool _complete;

        public CopyTaskManager(IPersistCopyTasks persistCopyTasks, IFileOperations fileOperations)
        {
            _allTaskDetails = new ConcurrentBag<CopyTaskDetails>();
            _tasksByExtension = new ConcurrentDictionary<string, ConcurrentQueue<CopyTaskDetails>>();
            _canProcess = new AutoResetEvent(false);
            _extensionLocks = new Dictionary<string, SemaphoreSlim>();
            _persistCopyTasks = persistCopyTasks;
            _complete = false;
            _fileOperations = fileOperations;
        }

        public async Task Start()
        {
            await _persistCopyTasks.Init();
            var incompleteTasks = _persistCopyTasks.GetIncompleteCopyTasks();
            foreach (var incompleteTask in incompleteTasks)
            {
                AddCopyTaskDetails(incompleteTask);
            }
            _manageCompletionTask = Task.Run(() => ManageCompletion());
        }

        public async Task End()
        {
            _complete = true;
            _canProcess.Set();
            await _manageCompletionTask.WaitAsync(TimeSpan.FromSeconds(WaitTimeInSeconds));
        }

        public void AddDirectories(string source, string destination)
        {
            Task.Run(() => CreateCopyTasks(source, destination));
        }

        public List<CopyTaskDetails> GetStatus()
        {
            return _allTaskDetails.ToList();
        }

        private async Task CreateCopyTasks(string source, string destination)
        {
            try
            {
                foreach (var copyTaskDetails in _fileOperations.GetTasksFromDirectory(source, destination))
                {
                    AddCopyTaskDetails(copyTaskDetails);
                    await _persistCopyTasks.AddCopyTask(copyTaskDetails).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _allTaskDetails.Add(new CopyTaskDetails(
                    source,
                    destination,
                    "directory")
                {
                    TransferStatus = TransferStatus.Error,
                    ErrorMessage = e.Message
                });
            }
            _canProcess.Set();
        }

        private void AddCopyTaskDetails(CopyTaskDetails copyTaskDetails)
        {
            _allTaskDetails.Add(copyTaskDetails);
            if (!_extensionLocks.ContainsKey(copyTaskDetails.Extension))
            {
                _extensionLocks.Add(copyTaskDetails.Extension, new SemaphoreSlim(1));
            }
            _tasksByExtension.GetOrAdd(copyTaskDetails.Extension, new ConcurrentQueue<CopyTaskDetails>()).Enqueue(copyTaskDetails);
        }

        private async Task RunCopy(CopyTaskDetails copyTaskDetails)
        {
            try
            {
                await SetState(copyTaskDetails, TransferStatus.Copying).ConfigureAwait(false);

                await _persistCopyTasks.AddCopyTask(copyTaskDetails).ConfigureAwait(false);
                _fileOperations.CopyFile(copyTaskDetails);

                await SetState(copyTaskDetails, TransferStatus.Done).ConfigureAwait(false);
                await _persistCopyTasks.RemoveCopyTask(copyTaskDetails).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                await SetState(copyTaskDetails, TransferStatus.Error, e.ToString()).ConfigureAwait(false);
                try
                {
                    await _persistCopyTasks.RemoveCopyTask(copyTaskDetails).ConfigureAwait(true);
                }
                catch (Exception) 
                {
                }
            }
            finally
            {
                _canProcess.Set();
            }
        }

        private async Task SetState(CopyTaskDetails copyTaskDetails, TransferStatus newState, string errorMessage = null)
        {
            await _extensionLocks[copyTaskDetails.Extension].WaitAsync().ConfigureAwait(false);
            copyTaskDetails.TransferStatus = newState;
            copyTaskDetails.ErrorMessage = errorMessage;
            copyTaskDetails.HistoricalTransferStatuses.Add(newState, DateTime.UtcNow);
            _extensionLocks[copyTaskDetails.Extension].Release();
        }

        private async Task ManageCompletion()
        {

            while (true)
            {
                while (!_canProcess.WaitOne(TimeSpan.FromSeconds(WaitTimeInSeconds)))
                {
                    if (_complete)
                    {
                        return;
                    }
                }
                if (_complete)
                {
                    return;
                }
                foreach (var extension in _tasksByExtension)
                {
                    try
                    {
                        if (!extension.Value.TryPeek(out var copyTaskDetails))
                        {
                            continue;
                        }

                        await _extensionLocks[copyTaskDetails.Extension].WaitAsync().ConfigureAwait(false);
                        if (copyTaskDetails.TransferStatus == TransferStatus.Done || copyTaskDetails.TransferStatus == TransferStatus.Error)
                        {
                            extension.Value.TryDequeue(out _);
                        }
                        _extensionLocks[copyTaskDetails.Extension].Release();
                        if (!extension.Value.TryPeek(out copyTaskDetails))
                        {
                            continue;
                        }

                        if (copyTaskDetails.TransferStatus == TransferStatus.Awaiting)
                        {
                            Task.Run(() => RunCopy(copyTaskDetails));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
        }

    }
}
