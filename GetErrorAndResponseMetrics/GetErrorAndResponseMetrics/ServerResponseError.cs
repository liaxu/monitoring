using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetErrorAndResponseMetrics
{
    class ServerResponseError
    {
        public int TimeStamp { get; set; }
        public int ResponseTime { get; set; }
        public int HTTP4XX { get; set; }
        public int HTTP5XX { get; set; }
        public int HTTP406 { get; set; }
        public int HTTP404 { get; set; }
        public int HTTP403 { get; set; }
        public int HTTP401 { get; set; }
        public int HTTP2XX { get; set; }
        public int HTTP3XX { get; set; }
        public int Requests { get; set; }
    }
}
