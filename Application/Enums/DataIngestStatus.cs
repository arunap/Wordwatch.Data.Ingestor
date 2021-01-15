﻿namespace Wordwatch.Data.Ingestor.Application.Enums
{
    public enum DataIngestStatus
    {
        Pending = 0,
        Started = 1,
        Stopped = 2,
        Completed = 3
    }

    public enum SyncTableNames
    {
        Calls = 0,
        MediaStubs = 1,
        VoxStubs = 2
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

        CallLastSyncedAt  = 7,
        MediaStubsLastSyncedAt = 8,
        VoxStubsLastSyncedAt = 9,
        CallsMinDate = 10,
        CallsMaxDate = 11,
    }
}
