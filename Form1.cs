using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Wordwatch.Data.Ingestor.Application.Enums;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Implementation;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor
{
    public partial class Form1 : Form
    {
        private Dictionary<UIFields, string> _keyValuePair;
        private readonly ApplicationSettings _applicationSettings;
        private readonly ILogger<Form1> _logger;
        private readonly IApplicationDbContext _sourceDbContext;
        private readonly IApplicationDbContext _targetDbContext;
        private readonly IDataIngestor _dataIngestor;
        private IProgress<CallIngestorInfo> _progressCallback;

        private Form1()
        {
            _progressCallback = new Progress<CallIngestorInfo>(p => UpdateProgress(p));
            _dataIngestor.RegisterProgressCallBacks(_progressCallback);

            InitializeComponent();
        }

        public Form1(ILogger<Form1> logger, IOptions<ApplicationSettings> applicationSettings)
        {
            _logger = logger;
            _applicationSettings = applicationSettings.Value;

            _sourceDbContext = new SourceDbContext(_applicationSettings);
            _targetDbContext = new DestinationDbContext(_applicationSettings);
            _dataIngestor = new DataIngestorService(_sourceDbContext, _targetDbContext);

            _progressCallback = new Progress<CallIngestorInfo>(p => UpdateProgress(p));
            _dataIngestor.RegisterProgressCallBacks(_progressCallback);

            InitializeComponent();
            _logger.LogInformation("started!");
        }

        private void UpdateProgress(CallIngestorInfo info)
        {
            ListViewItem viewItem = new ListViewItem();
            viewItem.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            viewItem.SubItems.Add(info.Message);

            listView1.Items.Add(viewItem);

            listView1.EnsureVisible(listView1.Items.Count - 1);
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            // buttonStart.Enabled = false;
            await _dataIngestor.StartAync(default);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            _keyValuePair = new Dictionary<UIFields, string>();
            foreach (UIFields foo in Enum.GetValues(typeof(UIFields)))
            {
                _keyValuePair.Add(foo, "");
            }

            CallIngestorService service = new CallIngestorService(_sourceDbContext, _targetDbContext, _applicationSettings);
            await service.ExecuteIterationsAsync(Literals.SyncTableNames.CallsTable, 2556, UpdateUI);
        }

        private void UpdateUI(ProgressNotifier notifier)
        {
            if (!string.IsNullOrEmpty(notifier.Message))
            {
                ListViewItem viewItem = new ListViewItem { Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                viewItem.SubItems.Add(notifier.Message);
                listView1.Items.Add(viewItem);
                listView1.EnsureVisible(listView1.Items.Count - 1);

                _logger.LogInformation(notifier.Message);
            }

            if (notifier.FieldValue == null)
                return;

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

            labelSource.Text = sourceText.ToString();

            int.TryParse(_keyValuePair[UIFields.TargetIngestedCallCount], out int ingetedCalls);
            int.TryParse(_keyValuePair[UIFields.TargetIngestedMediaStubCount], out int ingestedMedia);
            int.TryParse(_keyValuePair[UIFields.TargetIngestedVoxStubCount], out int ingestedVox);


            DateTimeOffset.TryParse(_keyValuePair[UIFields.CallLastSyncedAt], out DateTimeOffset synced);
            DateTimeOffset.TryParse(_keyValuePair[UIFields.CallsMaxDate], out DateTimeOffset maxDate);

            StringBuilder targetText = new StringBuilder();
            targetText.Append($"Calls: {ingetedCalls:N0}, Synced On: {_keyValuePair[UIFields.CallLastSyncedAt]}{Environment.NewLine}");
            targetText.Append($"MediaStubs: {ingestedMedia:N0} Synced On: {_keyValuePair[UIFields.MediaStubsLastSyncedAt]}{Environment.NewLine}");
            targetText.Append($"VoxStubs: {ingestedVox:N0} Synced On: {_keyValuePair[UIFields.VoxStubsLastSyncedAt]}{Environment.NewLine}");
            targetText.Append($"Days Pending: {Math.Round((maxDate - synced).TotalDays)} ({(sourceCallCount - ingetedCalls).ToString("N0")}) ");
            labelTarget.Text = targetText.ToString();
        }
    }
}
