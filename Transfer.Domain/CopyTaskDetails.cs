namespace Transfer.Domain
{
    public class CopyTaskDetails : IEquatable<CopyTaskDetails>
    {
        public CopyTaskDetails(string source, string destination, string extension)
        {
            Source = source;
            Destination = destination;
            TransferStatus = TransferStatus.Awaiting;
            ErrorMessage = null;
            Extension = extension;
            HistoricalTransferStatuses = new Dictionary<TransferStatus, DateTime> { { TransferStatus.Awaiting, DateTime.UtcNow } };
        }

        public string Source { get; }

        public string Destination { get; }

        public string Extension { get; }

        public TransferStatus TransferStatus { get; set; }

        public Dictionary<TransferStatus, DateTime> HistoricalTransferStatuses { get; set; }

        public string? ErrorMessage { get; set; }

        public bool Equals(CopyTaskDetails? other)
        {
            if(ReferenceEquals(other, null))
            {
                return false;
            }
            
            if(ReferenceEquals(other, this))
            {
                return true;
            }
            return other.Destination == this.Destination && other.Source == this.Source;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as CopyTaskDetails);
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode() ^ Destination.GetHashCode();
        }

        public override string ToString()
        {
            return $"Source: {Source}, Destination: {Destination}, Transfer Status {TransferStatus}, Error Message: {ErrorMessage}"; 
        }
    }
}
