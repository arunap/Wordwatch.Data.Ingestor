﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Implementation;

namespace Wordwatch.Data.Ingestor.Application.Interfaces
{
    public interface IMigrationActionService
    {
        public Task InitAsync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken = default);

        public Task StartAync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken = default);

        public Task Pause(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken = default);

        public Task ResumeAync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken = default);

        public Task StopAync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken = default);

        public Task ExitAync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken = default);

        public event EventHandler<DataIngestStatusEvent> WorkflowStateChanged;
    }
}
