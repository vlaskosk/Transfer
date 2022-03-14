namespace Transfer.Domain
{
    public class CommandsProcessor
    {
        private readonly ICopyTaskManager _copyTaskManager;
        private readonly IAddCommandDirectoriesBuilder _addCommandDirectoriesBuilder;

        public CommandsProcessor(ICopyTaskManager copyTaskManager, IAddCommandDirectoriesBuilder addCommandDirectoriesBuilder)
        {
            _copyTaskManager = copyTaskManager;
            _addCommandDirectoriesBuilder = addCommandDirectoriesBuilder;
        }

        public async Task<(bool isFinished, string message)> ExecuteCommand(string command)
        {
            if(command == null || command.Length == 0)
            {
                return (false, "No Command");
            }

            var splitCommand = command.Split(' ');
            if(splitCommand.Length >= 3 && string.Equals(splitCommand[0], "add", StringComparison.InvariantCultureIgnoreCase))
            {
                var (sourceDir, destinationDir, okCommand) = _addCommandDirectoriesBuilder.BuildDirectories(command);
                if (okCommand)
                {
                    _copyTaskManager.AddDirectories(sourceDir, destinationDir);
                    return (false, $"Copying data from '{splitCommand[1]}' to '{splitCommand[2]}'");
                }
                else
                {
                    return (false, "Unknown Command");
                }
            }
            if(splitCommand.Length == 1 && string.Equals(splitCommand[0], "status", StringComparison.InvariantCultureIgnoreCase))
            {
                var status = "Copy Status:" + Environment.NewLine;
                status += string.Join(Environment.NewLine, _copyTaskManager.GetStatus().Select(e => e.ToString())); 
                return (false, status);
            }
            if (splitCommand.Length == 1 && string.Equals(splitCommand[0], "exit", StringComparison.InvariantCultureIgnoreCase))
            {
                await _copyTaskManager.End();
                return (true, "Exited");
            }
            return (false, "Unknown Command");
        }

    }
}
