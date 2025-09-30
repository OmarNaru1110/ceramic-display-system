namespace API.Configuration
{
    public class DefaultAdminOptions
    {
        public const string SectionName = "DefaultAdmin";
        
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}