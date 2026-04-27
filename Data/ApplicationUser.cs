using Microsoft.AspNetCore.Identity;

namespace secure_workflow_system.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; }
    }

}
