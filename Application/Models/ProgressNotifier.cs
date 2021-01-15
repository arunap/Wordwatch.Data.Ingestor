using Wordwatch.Data.Ingestor.Application.Enums;

namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class ProgressNotifier
    {
        public string Message { get; set; }

        public UIFields Field { get; set; }

        public object FieldValue { get; set; }
    }
}
