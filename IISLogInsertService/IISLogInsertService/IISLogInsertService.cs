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
using System.Timers;

namespace IISLogInsertService
{
    public partial class IISLogInsertService : ServiceBase
    {
        System.Timers.Timer _timer;     
        public IISLogInsertService()
        {
            InitializeComponent();
            _timer = new System.Timers.Timer();
        }

        protected override void OnStart(string[] args)
        {
            _timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            _timer.Interval = 20000;
            _timer.Enabled = true;
            _timer.Start();
        }

        private TimeSpan calcuteInterval()
        {
            return Convert.ToDateTime(DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-dd HH:00:00")).AddMinutes(1) - DateTime.UtcNow;
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Interval = calcuteInterval().TotalMilliseconds;
            IISLogInsert insertForDev = new IISLogInsert(false);
            Thread t = new Thread(new ParameterizedThreadStart(insertForDev.DownloadAndInsertCurrentHourLog));
            t.Start();
            Thread.Sleep(10000);
            IISLogInsert insertForGraph = new IISLogInsert(true);
            Thread t2 = new Thread(new ParameterizedThreadStart(insertForGraph.DownloadAndInsertCurrentHourLog));
            t2.Start();
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
        }
    }
}
