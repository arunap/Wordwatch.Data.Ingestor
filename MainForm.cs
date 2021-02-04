using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wordwatch.Data.Ingestor.Application.Helpers;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor
{
    public partial class MainForm : Form
    {
        private readonly ILogger<MainForm> _logger;
        private readonly IMigrationActionService _migrationActionService;
        private readonly ProgressObserverHelper _observerHelper = new ProgressObserverHelper();
        private IProgress<ProgressNotifier> _progressCallBack;
        private readonly ApplicationSettings _applicationSettings;
        public MainForm(ILogger<MainForm> logger, IMigrationActionService migrationActionService, IOptions<ApplicationSettings> applicationSettings)
        {
            InitializeComponent();

            _logger = logger;
            _migrationActionService = migrationActionService;
            _logger.LogInformation("InitializeComponent()!");

            _progressCallBack = new Progress<ProgressNotifier>(p => UpdateUI(p));
            _applicationSettings = applicationSettings.Value;

            _migrationActionService.WorkflowStateChanged += OnWorkflowStateChanged;
        }

        private void OnWorkflowStateChanged(object sender, Implementation.DataIngestStatusEvent e)
        {
            switch (e.DataIngestStatus)
            {
                case Application.Constants.DataIngestStatus.Pending:
                    SetActionButtonState(init: true);
                    break;

                case Application.Constants.DataIngestStatus.Ready:
                    SetActionButtonState();
                    break;

                case Application.Constants.DataIngestStatus.Started:
                    SetActionButtonState(startClicked: true);
                    break;

                case Application.Constants.DataIngestStatus.Stopped:
                    SetActionButtonState(stopClicked: true);
                    break;

                case Application.Constants.DataIngestStatus.Completed:
                    break;

                case Application.Constants.DataIngestStatus.Paused:
                    SetActionButtonState(pausedClicked: true);
                    break;

                case Application.Constants.DataIngestStatus.Resumed:
                    SetActionButtonState(resumeClicked: true);
                    break;

                case Application.Constants.DataIngestStatus.Finished:
                    SetActionButtonState();

                    var confirmed = MessageBox.Show("Are you sure you want to Exit Application?", "Exit Action!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirmed == DialogResult.Yes)
                        this.Close();

                    break;

                default:
                    break;
            }
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
                {
                    labelTarget.Text = notifier.TargetText;
                    _logger.LogInformation(notifier.TargetText.Replace(Environment.NewLine, " "));
                }
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

            buttonStart.Enabled = false;
            buttonPause.Enabled = false;
            buttonResume.Enabled = false;
            buttonStop.Enabled = false;
            buttonExit.Enabled = false;

            groupBoxSource.Text = $"Source -> {_applicationSettings.ConnectionStrings.Source.Substring(0, 50)}";
            groupBoxTarget.Text = $"Target -> {_applicationSettings.ConnectionStrings.Target.Substring(0, 50)}";

            await _migrationActionService.InitAsync(progress: _progressCallBack, cancellationToken: default);
            SetActionButtonState();
        }

        private void SetActionButtonState(bool init = false, bool startClicked = false, bool pausedClicked = false, bool resumeClicked = false, bool stopClicked = false, bool exitClicked = false)
        {
            if (startClicked)
            {
                buttonStart.Enabled = false;
                buttonPause.Enabled = true;
                buttonResume.Enabled = false;
                buttonStop.Enabled = true;
                buttonExit.Enabled = false;
            }
            else if (pausedClicked)
            {
                buttonStart.Enabled = false;
                buttonPause.Enabled = false;
                buttonResume.Enabled = true;
                buttonStop.Enabled = true;
                buttonExit.Enabled = false;
            }
            else if (resumeClicked)
            {
                buttonStart.Enabled = false;
                buttonPause.Enabled = true;
                buttonResume.Enabled = false;
                buttonStop.Enabled = true;
                buttonExit.Enabled = false;
            }
            else if (stopClicked)
            {
                buttonStart.Enabled = true;
                buttonPause.Enabled = false;
                buttonResume.Enabled = false;
                buttonStop.Enabled = false;
                buttonExit.Enabled = true;
            }
            else if (exitClicked)
            {
                buttonStart.Enabled = false;
                buttonPause.Enabled = false;
                buttonResume.Enabled = false;
                buttonStop.Enabled = false;
            }
            else if (init)
            {
                buttonStart.Enabled = false;
                buttonPause.Enabled = false;
                buttonResume.Enabled = false;
                buttonStop.Enabled = false;
                buttonExit.Enabled = false;
            }
            else
            {
                buttonStart.Enabled = true;
                buttonPause.Enabled = false;
                buttonResume.Enabled = false;
                buttonStop.Enabled = false;
                buttonExit.Enabled = true;
            }
        }

        private async Task ExecuteAsyncTask(Task task)
        {
            await Task.Run(async () => await task);
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            var confirmed = MessageBox.Show("Are you sure you want to Start Migration?", "Start Action!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmed == DialogResult.Yes)
            {
                await ExecuteAsyncTask(_migrationActionService.StartAync(progress: _progressCallBack, cancellationToken: default));
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            var confirmed = MessageBox.Show("Are you sure you want to Exit Application?", "Exit Action!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmed == DialogResult.Yes)
            {
                //await ExecuteAsyncTask(_migrationActionService.StopAync(progress: _progressCallBack, cancellationToken: default));
                this.Close();
            }
        }

        private async void buttonPause_Click(object sender, EventArgs e)
        {
            var confirmed = MessageBox.Show("Are you sure you want to Pause Migration?", "Pause Action!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmed == DialogResult.Yes)
            {
                await ExecuteAsyncTask(_migrationActionService.Pause(progress: _progressCallBack, cancellationToken: default));
            }
        }

        private async void buttonResume_Click(object sender, EventArgs e)
        {
            var confirmed = MessageBox.Show("Are you sure you want to Resume Migration?", "Resume Action!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmed == DialogResult.Yes)
            {
                await ExecuteAsyncTask(_migrationActionService.ResumeAync(progress: _progressCallBack, cancellationToken: default));
            }
        }

        private async void buttonStop_Click(object sender, EventArgs e)
        {
            var confirmed = MessageBox.Show("Are you sure you want to Stop Migration?", "Stop Action!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmed == DialogResult.Yes)
            {
                await ExecuteAsyncTask(_migrationActionService.StopAync(progress: _progressCallBack, cancellationToken: default));
            }
        }
    }
}
