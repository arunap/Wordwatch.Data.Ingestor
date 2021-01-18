﻿using System;
using System.Collections.Generic;
using System.Text;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor.Application.Helpers
{
    public class ProgressResults
    {
        public string Message { get; set; }
        public string SourceText { get; set; }
        public string TargetText { get; set; }
        public int CompletionValue { get; set; }
    }

    public class ProgressObserverHelper
    {
        private Dictionary<UIFields, string> _keyValuePair = new Dictionary<UIFields, string>();
        public ProgressObserverHelper()
        {
            foreach (UIFields foo in Enum.GetValues(typeof(UIFields)))
            {
                _keyValuePair.Add(foo, "");
            }
        }
        public ProgressResults WatchProgress(ProgressNotifier notifier)
        {
            if (notifier.FieldValue == null)
                return null;

            switch (notifier.Field)
            {
                case UIFields.TargetIngestedCallCount:
                case UIFields.TargetIngestedMediaStubCount:
                case UIFields.TargetIngestedVoxStubCount:

                    int.TryParse(notifier.FieldValue.ToString(), out int newValue);

                    if (_keyValuePair.ContainsKey(notifier.Field))
                    {
                        int.TryParse(_keyValuePair[notifier.Field], out int oldValue);
                        _keyValuePair[notifier.Field] = (oldValue + newValue).ToString();
                    }
                    break;

                case UIFields.CallLastSyncedAt:
                case UIFields.MediaStubsLastSyncedAt:
                case UIFields.VoxStubsLastSyncedAt:
                case UIFields.CallsMinDate:
                case UIFields.CallsMaxDate:

                    if (_keyValuePair.ContainsKey(notifier.Field))
                        _keyValuePair[notifier.Field] = ((DateTimeOffset)notifier.FieldValue).ToString("yyyy-MM-dd");
                    else
                        _keyValuePair.TryAdd(notifier.Field, ((DateTimeOffset)notifier.FieldValue).ToString("yyyy-MM-dd"));

                    break;

                default:
                    if (_keyValuePair.ContainsKey(notifier.Field))
                        _keyValuePair[notifier.Field] = notifier.FieldValue.ToString();
                    else
                        _keyValuePair.Add(notifier.Field, notifier.FieldValue.ToString());
                    break;
            }

            StringBuilder sourceText = new StringBuilder();

            int.TryParse(_keyValuePair[UIFields.SourceCallCount], out int sourceCallCount);
            int.TryParse(_keyValuePair[UIFields.SourceMediaStubCount], out int sourceMediaCount);
            int.TryParse(_keyValuePair[UIFields.SourceVoxStubCount], out int sourceVoxCount);

            sourceText.Append($"Calls: {sourceCallCount:N0} {Environment.NewLine}");
            sourceText.Append($"MediaStubs: {sourceMediaCount:N0} {Environment.NewLine}");
            sourceText.Append($"VoxStubs: {sourceVoxCount:N0} {Environment.NewLine}");
            sourceText.Append($"Dates: {_keyValuePair[UIFields.CallsMinDate]} - {_keyValuePair[UIFields.CallsMaxDate]} ({_keyValuePair[UIFields.SourceCallDistribution]})");

            int.TryParse(_keyValuePair[UIFields.TargetIngestedCallCount], out int ingetedCalls);
            int.TryParse(_keyValuePair[UIFields.TargetIngestedMediaStubCount], out int ingestedMedia);
            int.TryParse(_keyValuePair[UIFields.TargetIngestedVoxStubCount], out int ingestedVox);


            DateTimeOffset.TryParse(_keyValuePair[UIFields.CallLastSyncedAt], out DateTimeOffset synced);
            DateTimeOffset.TryParse(_keyValuePair[UIFields.CallsMaxDate], out DateTimeOffset maxDate);

            StringBuilder targetText = new StringBuilder();
            targetText.Append($"Calls: {ingetedCalls:N0}, Synced On: {_keyValuePair[UIFields.CallLastSyncedAt]}{Environment.NewLine}");
            targetText.Append($"MediaStubs: {ingestedMedia:N0} Synced On: {_keyValuePair[UIFields.MediaStubsLastSyncedAt]}{Environment.NewLine}");
            targetText.Append($"VoxStubs: {ingestedVox:N0} Synced On: {_keyValuePair[UIFields.VoxStubsLastSyncedAt]}{Environment.NewLine}");

            if (synced == DateTimeOffset.MinValue)
                targetText.Append($"Days Pending: {_keyValuePair[UIFields.SourceCallDistribution]} ({sourceCallCount - ingetedCalls:N0})");
            else
                targetText.Append($"Days Pending: { Math.Round((maxDate - synced).TotalDays)} ({sourceCallCount - ingetedCalls:N0}) ");

            var args = new ProgressResults
            {
                Message = notifier.Message,
                SourceText = sourceText.ToString(),
                TargetText = targetText.ToString(),
                CompletionValue = (ingetedCalls / sourceCallCount) * 100
            };

            return args;
        }
    }
}