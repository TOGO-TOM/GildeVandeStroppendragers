namespace AdminMembers.Models
{
    public class ExportRequest
    {
        public List<string> SelectedFields { get; set; } = new List<string>();
        public List<string>? FilterRoles { get; set; }
    }
}
