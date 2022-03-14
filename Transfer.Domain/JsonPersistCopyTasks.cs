using Newtonsoft.Json;
using System.Reflection;

namespace Transfer.Domain
{
    public class JsonPersistCopyTasks : IPersistCopyTasks
    {
        private static readonly string _jsonFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "CopyTasks.json"; 

        private readonly HashSet<CopyTaskDetails> _copyTaskDetails;

        private readonly SemaphoreSlim _lock;

        public JsonPersistCopyTasks()
        {
            _copyTaskDetails = new HashSet<CopyTaskDetails>();
            _lock = new SemaphoreSlim(1);
        }

        public async Task AddCopyTask(CopyTaskDetails addTask)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            _copyTaskDetails.Add(addTask);
            await WriteFile().ConfigureAwait(false);
            _lock.Release();
        }

        public List<CopyTaskDetails> GetIncompleteCopyTasks()
        {
            return _copyTaskDetails.Where(e => e.TransferStatus == TransferStatus.Copying || e.TransferStatus == TransferStatus.Awaiting).ToList();
        }

        public async Task Init()
        {
            if(!File.Exists(_jsonFile))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(_jsonFile).ConfigureAwait(false);
            try
            {
                var copyTasks = JsonConvert.DeserializeObject<List<CopyTaskDetails>>(json);
                _copyTaskDetails.Clear();
                if (copyTasks != null && copyTasks.Count > 0)
                {
                    _copyTaskDetails.UnionWith(copyTasks);
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        public async Task RemoveCopyTask(CopyTaskDetails taskToRemove)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            _copyTaskDetails.Remove(taskToRemove);
            await WriteFile().ConfigureAwait(false);
            _lock.Release();
        }

        private async Task WriteFile()
        {
            var json = JsonConvert.SerializeObject(_copyTaskDetails);
            await File.WriteAllTextAsync(_jsonFile, json).ConfigureAwait(false);

        }
    }
}
