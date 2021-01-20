using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Helpers;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor
{
    public partial class MainForm : Form
    {
        private Dictionary<UIFields, string> _keyValuePair;
        private readonly ILogger<MainForm> _logger;
        private readonly IMigrationActionService _migrationActionService;
        private readonly ProgressObserverHelper _observerHelper = new ProgressObserverHelper();
        private IProgress<ProgressNotifier> _progressCallBack;

        public MainForm(ILogger<MainForm> logger, IMigrationActionService migrationActionService)
        {
            InitializeComponent();

            _logger = logger;
            _migrationActionService = migrationActionService;
            _logger.LogInformation("InitializeComponent()!");

            _progressCallBack = new Progress<ProgressNotifier>(p => UpdateUI(p));
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            await Task.Run(async () =>
            {
                await _migrationActionService.StartAync(progress: _progressCallBack, cancellationToken: default);
            });
        }

        public void UpdateUI(ProgressNotifier values)
        {
            ProgressResults notifier = _observerHelper.WatchProgress(values);

            if (notifier != null)
            {
                if (!string.IsNullOrEmpty(notifier.Message))
                {
                    ListViewItem viewItem = new ListViewItem { Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                    viewItem.SubItems.Add(notifier.Message);
                    listView1.Items.Add(viewItem);
                    listView1.EnsureVisible(listView1.Items.Count - 1);

                    _logger.LogInformation(notifier.Message);
                }

                if (!string.IsNullOrEmpty(notifier.SourceText))
                    labelSource.Text = notifier.SourceText;

                if (!string.IsNullOrEmpty(notifier.TargetText))
                    labelTarget.Text = notifier.TargetText;

                if (notifier.CompletionValue > 0)
                {
                    progressBar1.Value = notifier.CompletionValue;
                    labelProgress.Text = notifier.CompletionValue.ToString() + "%";
                }
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            _logger.LogInformation("MainForm_Load()!");

            _keyValuePair = new Dictionary<UIFields, string>();
            foreach (UIFields foo in Enum.GetValues(typeof(UIFields)))
            {
                _keyValuePair.Add(foo, "");
            }

            await Task.Run(async () =>
            {
                await _migrationActionService.InitAsync(progress: _progressCallBack, cancellationToken: default);
            });
        }
    }
}
