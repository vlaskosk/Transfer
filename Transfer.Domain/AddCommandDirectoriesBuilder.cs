namespace Transfer.Domain
{
    public class AddCommandDirectoriesBuilder : IAddCommandDirectoriesBuilder
    {
        public (string sourceDir, string destinationDir, bool okCommand) BuildDirectories(string command)
        {
            var splitCommand = command.Split(' ');
            string sourceDir = splitCommand[1];

            var destinationDir = "";
            var countOfQuotes = command.Count(e => e == '"');
            if ((splitCommand.Length > 3 && countOfQuotes == 0) || countOfQuotes % 2 == 1)
            {
                return ("", "", false);
            }
            if (splitCommand.Length == 3)
            {
                destinationDir = splitCommand[2];

                if ((!sourceDir.Contains("\"") || (sourceDir.StartsWith("\"") && sourceDir.EndsWith("\"")))
                    && (!destinationDir.Contains("\"") || (destinationDir.StartsWith("\"") && destinationDir.EndsWith("\""))))
                {
                    sourceDir = sourceDir.Replace("\"", "");
                    destinationDir = destinationDir.Replace("\"", "");
                    return (sourceDir, destinationDir, true);
                }
                else
                {
                    return ("", "", false);
                }
            }
            if (!sourceDir.StartsWith("\""))
            {
                for (int i = 2; i < splitCommand.Length; i++)
                {
                    destinationDir += " " + splitCommand[i];
                }
            }
            else
            {
                var sourceEnd = 2;
                for (int i = sourceEnd; i < splitCommand.Length; i++)
                {
                    sourceEnd = i;
                    sourceDir += " " + splitCommand[i];
                    if (splitCommand[i].EndsWith("\""))
                    {
                        break;
                    }
                }
                if (sourceEnd == splitCommand.Length - 1)
                {
                    return ("", "", false);
                }

                for (int i = sourceEnd + 1; i < splitCommand.Length; i++)
                {
                    destinationDir += " " + splitCommand[i];
                }
            }
            sourceDir = sourceDir.Replace("\"", "");
            destinationDir = destinationDir.Replace("\"", "");
            if (destinationDir.StartsWith(" "))
            {
                destinationDir = destinationDir.Substring(1, destinationDir.Length - 1);
            }
            return (sourceDir, destinationDir, true);
        }
    }
}
