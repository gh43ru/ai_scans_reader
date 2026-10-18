namespace ai_scans_reader.Models
{
    public class ScanPersonResult
    {
        public string FullName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;

        public string PersonnelNumber { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string DocumentDate { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string HireDate { get; set; } = string.Empty;
        public string Salary { get; set; } = string.Empty;
        public string EmploymentType { get; set; } = string.Empty;
        public string ProbationPeriod { get; set; } = string.Empty;
    }
}