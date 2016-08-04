
namespace IISLogDowload.Model
{
    using System;
    using System.Collections.Generic;

    public partial class t_iis_log_dev_test
    {
        public int Id { get; set; }
        public int TimeStamp { get; set; }
        public string URI { get; set; }
        public string HOST { get; set; }
        public string Referer { get; set; }
        public string IP { get; set; }
        public string ai_session { get; set; }
        public string ai_user { get; set; }
        public string status { get; set; }
        public Nullable<int> time_taken { get; set; }
    }
}
