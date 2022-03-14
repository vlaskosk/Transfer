using System;
using Transfer.Domain;

namespace Transfer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("This program must be started with following arguments:");
                Console.WriteLine("Transfer sourcedirectory destinationdirectory");
                return;
            }

            var copyTaskManager = new CopyTaskManager(new JsonPersistCopyTasks(), new FileOperations());
            await copyTaskManager.Start();
            copyTaskManager.AddDirectories(args[0], args[1]);
            var commandProcessor = new CommandsProcessor(copyTaskManager, new AddCommandDirectoriesBuilder());
            while (true)
            {
                Console.WriteLine("Following commands are available:");
                Console.WriteLine("add sourcedirectory destinationdirectory");
                Console.WriteLine("status");
                Console.WriteLine("exit");
                Console.WriteLine("Please enter command");
                var command = Console.ReadLine();
                var result = await commandProcessor.ExecuteCommand(command).ConfigureAwait(false);
                Console.WriteLine(result.message);
                if (result.isFinished)
                {
                    break;
                }
            }

        }
    }
}