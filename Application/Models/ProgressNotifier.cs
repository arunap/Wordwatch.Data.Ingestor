using Wordwatch.Data.Ingestor.Application.Constants;

namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class ProgressNotifier
    {
        public string Message { get; set; }

        public UIFields Field { get; set; }

        public object FieldValue { get; set; }
    }
}
