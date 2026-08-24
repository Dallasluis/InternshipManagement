namespace InternshipManagement.Web.Models.Student
{
    public class AddSkillRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ProficiencyLevel { get; set; }
    }
}