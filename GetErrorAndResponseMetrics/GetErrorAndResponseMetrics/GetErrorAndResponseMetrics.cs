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

namespace GetErrorAndResponseMetrics
{
    public partial class GetErrorAndResponseMetrics : ServiceBase
    {
        public GetErrorAndResponseMetrics()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            ParserAndInsertDB graphPerDay = new ParserAndInsertDB(true);
            Thread graphPerDayThread= new Thread(new ParameterizedThreadStart(graphPerDay.GetMetricsAndInsertDB));
            graphPerDayThread.Start(false);

            ParserAndInsertDB devPerDay = new ParserAndInsertDB(false);
            Thread devPerDayThread= new Thread(new ParameterizedThreadStart(devPerDay.GetMetricsAndInsertDB));
            devPerDayThread.Start(false);

            ParserAndInsertDB graphPerHour = new ParserAndInsertDB(true);
            Thread graphPerHourThread = new Thread(new ParameterizedThreadStart(graphPerHour.GetMetricsAndInsertDB));
            graphPerHourThread.Start(true);

            ParserAndInsertDB devPerHour = new ParserAndInsertDB(false);
            Thread devPerHourThread = new Thread(new ParameterizedThreadStart(devPerHour.GetMetricsAndInsertDB));
            devPerHourThread.Start(true);
        }

        protected override void OnStop()
        {
        }
    }
}
