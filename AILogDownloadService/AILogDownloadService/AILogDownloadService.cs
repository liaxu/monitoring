using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AILogDownloadService
{
    public partial class AILogDownloadService : ServiceBase
    {
        public AILogDownloadService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            DownloadLog downloadLogForDev = new DownloadLog();
            Thread t = new Thread(new ParameterizedThreadStart(downloadLogForDev.downloadLog));
            t.Start(false);

            DownloadLog downloadLogForGraph = new DownloadLog();
            Thread t2 = new Thread(new ParameterizedThreadStart(downloadLogForGraph.downloadLog));
            t2.Start(true);
        }

        protected override void OnStop()
        {
            
        }
    }
}
