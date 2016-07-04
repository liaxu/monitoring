using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServerLog2DB
{
    class Record
    {
        public int id { get; set; }
        public int status { get; set; } 
        public string ip { get; set; }
        public string url { get; set; }
        public string refurl { get; set; }
        public long timeString { get; set; }
        public int actionTime { get; set; }
        public long dayTime { get; set; }
        public long hourTime { get; set; }

        public Record()
        {
            status = -1;
        }
    }
}
