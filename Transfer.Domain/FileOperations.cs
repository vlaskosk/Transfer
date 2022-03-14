namespace Transfer.Domain
{
    public class FileOperations : IFileOperations
    {
        public List<CopyTaskDetails> GetTasksFromDirectory(string source, string destination)
        {
            var cleanedSource = source.Replace("\"", "");
            var cleanedDestination = destination.Replace("\"", "");
            if (!Directory.Exists(cleanedSource))
            {
                throw new Exception($"Source location {cleanedSource} does not exist");
            }

            if (!Directory.Exists(cleanedDestination))
            {
                throw new Exception($"Destination location {cleanedDestination} does not exist");
            }

            var result = new List<CopyTaskDetails>();
            foreach (var fileName in Directory.EnumerateFiles(cleanedSource).Select(Path.GetFileName))
            {
                var copyTaskDetails = new CopyTaskDetails(
                    cleanedSource + Path.DirectorySeparatorChar + fileName,
                    cleanedDestination + Path.DirectorySeparatorChar + fileName,
                    Path.GetExtension(fileName))
                {
                    TransferStatus = TransferStatus.Awaiting
                };
                result.Add(copyTaskDetails);
            }
            return result;
        }

        public void CopyFile(CopyTaskDetails copyTaskDetails)
        {
            File.Copy(copyTaskDetails.Source, copyTaskDetails.Destination, true);
        }

    }
}
