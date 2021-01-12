using System;
using System.ComponentModel;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;
using Wordwatch.Data.Ingestor.Implementation;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor
{
    public partial class Form1 : Form
    {
        private readonly IApplicationDbContext _sourceDbContext;
        private readonly IApplicationDbContext _targetDbContext;
        private readonly IDataIngestor _dataIngestor;
        private IProgress<CallIngestorInfo> _progressCallback;
        SourceDataSummary _sourceData = new SourceDataSummary();

        public Form1()
        {
            _sourceDbContext = new SourceDbContext();
            _targetDbContext = new DestinationDbContext();
            _dataIngestor = new DataIngestorService(_sourceDbContext, _targetDbContext);

            _progressCallback = new Progress<CallIngestorInfo>(p => UpdateProgress(p));
            _dataIngestor.RegisterProgressCallBacks(_progressCallback);

            InitializeComponent();
        }

        private Task LoadSourceInfo(BackgroundWorker worker)
        {
            worker.ReportProgress(1);
            //UpdateProgress(new CallIngestorInfo { Message = $"Started - Loading source call tables data information." });
            _sourceData.TotalCalls = _sourceDbContext.TableRowCountAsync<Call>().Result;
            worker.ReportProgress(33);

            //  UpdateProgress(new CallIngestorInfo { Message = $"Started - Loading source media stubs tables data information." });
            _sourceData.TotalMediaStubs = _sourceDbContext.TableRowCountAsync<MediaStub>().Result;
            worker.ReportProgress(66);

            // UpdateProgress(new CallIngestorInfo { Message = $"Started - Loading source vox stubs tables data information." });
            _sourceData.TotalVoxStubs = _sourceDbContext.TableRowCountAsync<VoxStub>().Result;
            worker.ReportProgress(100);

            // UpdateProgress(new CallIngestorInfo { Message = "Completed - Loading source tables data information." });
            return Task.CompletedTask;
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
            var _sourceData = await _dataIngestor.GetSourceDataSummaryAsync();
            labelSource.Text = _sourceData?.ToString();

            //try
            //{
            //    UpdateProgress(new CallIngestorInfo { Message = $"Started - Loading source tables data information." });

            //    var _sourceData = await _dataIngestor.GetSourceDataSummaryAsync();
            //    Thread.Sleep(TimeSpan.FromSeconds(5));
            //    labelSource.Text = _sourceData?.ToString();

            //    UpdateProgress(new CallIngestorInfo { Message = "Completed - Loading source tables data information." });
            //}
            //catch (Exception ex)
            //{
            //    UpdateProgress(new CallIngestorInfo { Message = $"Error - {ex.Message}." });
            //}
        }
    }
}
