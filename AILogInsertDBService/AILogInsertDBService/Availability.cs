using System;
using System.Collections.Generic;

namespace AILogInsertDBService
{
    public class AILog
    {
        public Availability availability { get; set; }
    }
    
    public class Availability
    {
        public string testName { get; set; }
        public string runLocation { get; set; }
        public string result { get; set; }
        public string testRunId { get; set; }
        public DateTime testTimestamp { get; set; }
        public DurationMetric durationMetric { get; set; }
        public AvailabilityMetric availabilityMetric { get; set; }
        public string message { get; set; }
        public DataSizeMetric dataSizeMetric { get; set; }
        public int count { get; set; }
    }

    public class DurationMetric
    {
        public string name { get; set; }
        public float value { get; set; }
        public float count { get; set; }
        public float min { get; set; }
        public float max { get; set; }
        public float stdDev { get; set; }
        public float sampledVlue { get; set; }
    }

    public class AvailabilityMetric
    {
        public string name { get; set; }
        public float value { get; set; }
        public float count { get; set; }
        public float min { get; set; }
        public float max { get; set; }
        public float stdDev { get; set; }
        public float sampledVlue { get; set; }
    }

    public class DataSizeMetric
    {
        public string name { get; set; }
        public float value { get; set; }
        public float count { get; set; }
    }
}
