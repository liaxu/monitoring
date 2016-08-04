using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISLogDownloadService
{
    class LastFile
    {
        public string localFileName { get; set; }
        public DateTime lastModifiedDate { get; set; }
        public int lastLine { get; set; }
        public bool isFinished { get; set; }
        public string name { get; set; }

        public LastFile(string name,string localFileName, DateTime lastModifiedDate, int lastLine, bool isFinished)
        {
            this.name = name;
            this.localFileName = localFileName;
            this.lastModifiedDate = lastModifiedDate;
            this.lastLine = lastLine;
            this.isFinished = isFinished;
        }

        public LastFile(string item, DateTime ExportStartDateTime)
        {
            this.name = item;
            this.localFileName = "";
            this.lastModifiedDate = ExportStartDateTime;
            this.lastLine = 0;
            this.isFinished = false;
        }
    }
}
