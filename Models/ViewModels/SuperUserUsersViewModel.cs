using System;
using System.Collections.Generic;

namespace JobTracker.Models.ViewModels
{
    public class SuperUserUserRowViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsDisabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }

    public class SuperUserUsersViewModel
    {
        public IReadOnlyList<SuperUserUserRowViewModel> Users { get; set; } = Array.Empty<SuperUserUserRowViewModel>();
    }
}
