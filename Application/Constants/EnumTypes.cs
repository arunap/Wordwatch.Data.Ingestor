namespace Wordwatch.Data.Ingestor.Application.Constants
{
    public enum DataIngestStatus
    {
        Pending = 0,
        Started = 1,
        Stopped = 2,
        Completed = 3,
        Paused = 4,
        Resumed = 5,
        Finished = 6,
        Ready = 7
    }

    public enum MigrationMessageActions
    {
        Started,
        Completed,
        Error,
        Reading,
        Migrated,
        Migrating
    }

    public enum UIFields
    {
        SourceCallDistribution = 0,

        SourceCallCount = 1,
        TargetIngestedCallCount = 2,

        SourceMediaStubCount = 3,
        TargetIngestedMediaStubCount = 4,

        SourceVoxStubCount = 5,
        TargetIngestedVoxStubCount = 6,

        CallLastSyncedAt = 7,
        MediaStubsLastSyncedAt = 8,
        VoxStubsLastSyncedAt = 9,
        CallsMinDate = 10,
        CallsMaxDate = 11,
    }

    public enum DbContextType { Source, Target }

    public enum IndexManageStatus { Enable, Disable }
}
