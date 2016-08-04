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

namespace AILogInsertDBService
{
    public partial class AILogInsertDBService : ServiceBase
    {
        public AILogInsertDBService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ParserAndInsertDB parserAndInsertDBForDev = new ParserAndInsertDB();
            Thread t = new Thread(new ParameterizedThreadStart(parserAndInsertDBForDev.ParserAILogAndInsertDB));
            t.Start(false);

            ParserAndInsertDB parserAndInsertDBForGraph = new ParserAndInsertDB();
            Thread t2 = new Thread(new ParameterizedThreadStart(parserAndInsertDBForGraph.ParserAILogAndInsertDB));
            t2.Start(true);
        }

        protected override void OnStop()
        {
        }
    }
}
