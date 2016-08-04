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

namespace GetServicePlanMetrics
{
    public partial class GetSerivePlanMetrics : ServiceBase
    {
        public GetSerivePlanMetrics()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ParserAndInsertDB graphPerMinute = new ParserAndInsertDB(true);
            Thread graphPerMinuteThread = new Thread(new ParameterizedThreadStart(graphPerMinute.GetMetricsAndInsertDB));
            graphPerMinuteThread.Start(false);

            ParserAndInsertDB graphPerHour = new ParserAndInsertDB(true);
            Thread graphPerHourThread= new Thread(new ParameterizedThreadStart(graphPerHour.GetMetricsAndInsertDB));
            graphPerHourThread.Start(true);

            ParserAndInsertDB devPerHour = new ParserAndInsertDB(false);
            Thread devPerHourThread= new Thread(new ParameterizedThreadStart(devPerHour.GetMetricsAndInsertDB));
            devPerHourThread.Start(true);

            ParserAndInsertDB devPerMinute = new ParserAndInsertDB(false);
            Thread devPerMinuteThread = new Thread(new ParameterizedThreadStart(devPerMinute.GetMetricsAndInsertDB));
            devPerMinuteThread.Start(false);

        }

        protected override void OnStop()
        {
        }
    }
}
