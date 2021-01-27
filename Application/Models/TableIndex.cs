namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class TableIndex
    {
        public string TableName { get; set; }
        public string IndexName { get; set; }
        public string EnableQuery { get; set; }
        public string DisableQuery { get; set; }
        public bool IsDisabled { get; set; }
    }
}
