using Microsoft.AspNetCore.Identity;

namespace secure_workflow_system.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; }

        // Access request details supplied at registration
        public string? FullName { get; set; }
        public string? Company { get; set; }
        public string? AccessReason { get; set; }
        public DateTime? RegisteredAtUtc { get; set; }
    }

}
