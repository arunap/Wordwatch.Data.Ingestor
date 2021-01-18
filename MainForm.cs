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
        private readonly IProgress<ProgressNotifier> _progressCallBack;
        private readonly ProgressObserverHelper _observerHelper = new ProgressObserverHelper();

        public MainForm(ILogger<MainForm> logger, IMigrationActionService migrationActionService)
        {
            InitializeComponent();

            _logger = logger;
            _migrationActionService = migrationActionService;
            _progressCallBack = new Progress<ProgressNotifier>(progress => UpdateUI(progress));

            _logger.LogInformation("InitializeComponent()!");
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
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

                labelSource.Text = notifier.SourceText;
                labelTarget.Text = notifier.TargetText;
                progressBar1.Value = notifier.CompletionValue;
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
