namespace AdminMembers.Services
{
    public class PasswordPolicyService
    {
        private readonly IConfiguration _configuration;

        public PasswordPolicyService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (bool Valid, string Message) Validate(string password)
        {
            var minLength        = _configuration.GetValue<int>("PasswordPolicy:MinLength", 8);
            var requireUpper     = _configuration.GetValue<bool>("PasswordPolicy:RequireUppercase", true);
            var requireLower     = _configuration.GetValue<bool>("PasswordPolicy:RequireLowercase", true);
            var requireDigit     = _configuration.GetValue<bool>("PasswordPolicy:RequireDigit", true);
            var requireSpecial   = _configuration.GetValue<bool>("PasswordPolicy:RequireSpecialChar", true);

            if (password.Length < minLength)
                return (false, $"Password must be at least {minLength} characters.");
            if (requireUpper && !password.Any(char.IsUpper))
                return (false, "Password must contain at least one uppercase letter.");
            if (requireLower && !password.Any(char.IsLower))
                return (false, "Password must contain at least one lowercase letter.");
            if (requireDigit && !password.Any(char.IsDigit))
                return (false, "Password must contain at least one digit.");
            if (requireSpecial && !password.Any(c => !char.IsLetterOrDigit(c)))
                return (false, "Password must contain at least one special character (e.g. !@#$%^&*).");

            return (true, string.Empty);
        }

        public string GetRequirementsHint()
        {
            var minLength      = _configuration.GetValue<int>("PasswordPolicy:MinLength", 8);
            var requireUpper   = _configuration.GetValue<bool>("PasswordPolicy:RequireUppercase", true);
            var requireLower   = _configuration.GetValue<bool>("PasswordPolicy:RequireLowercase", true);
            var requireDigit   = _configuration.GetValue<bool>("PasswordPolicy:RequireDigit", true);
            var requireSpecial = _configuration.GetValue<bool>("PasswordPolicy:RequireSpecialChar", true);

            var parts = new List<string> { $"at least {minLength} characters" };
            if (requireUpper)   parts.Add("1 uppercase");
            if (requireLower)   parts.Add("1 lowercase");
            if (requireDigit)   parts.Add("1 digit");
            if (requireSpecial) parts.Add("1 special character (!@#$%^&*)");

            return "Password must contain: " + string.Join(", ", parts) + ".";
        }
    }
}
