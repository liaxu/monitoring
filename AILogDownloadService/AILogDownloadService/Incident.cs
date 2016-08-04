namespace AILogDownloadService
{
    public class Incident
    {
        public int Id { get; set; }
        public int DependServiceId { get; set; }
        public int ImpactServiceId { get; set; }
        public string WebTestName { get; set; }
        public string ApplicationInsightName { get; set; }
        public string AlertMessage { get; set; }
        public string AlertReason { get; set; }
        public long AlertTime { get; set; }
        public long HealthTime { get; set; }
        public long Duration { get; set; }
        public string Location { get; set; }
        public string Solution { get; set; }
        public string AssignTo { get; set; }
        public string FixedBy { get; set; }
    }
}
