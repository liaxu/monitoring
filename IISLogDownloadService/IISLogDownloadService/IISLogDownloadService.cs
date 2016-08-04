using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IISLogDownloadService
{
    public partial class IISLogDownloadService : ServiceBase
    {
        public static Thread t;

        public IISLogDownloadService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            IISLogDownload downloadLogForDev = new IISLogDownload();
            t = new Thread(new ParameterizedThreadStart(downloadLogForDev.downloadLog));
            t.Start(false);
            //Thread.Sleep(5000);
            //t.Abort();
            IISLogDownload downloadLogForGraph = new IISLogDownload();
            Thread t2 = new Thread(new ParameterizedThreadStart(downloadLogForGraph.downloadLog));
            t2.Start(true);

        }

        protected override void OnStop()
        {

        }
    }
}
