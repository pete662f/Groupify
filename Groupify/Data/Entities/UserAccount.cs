using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Groupify.Data.Entities
{
    public class UserAccount : IdentityUser
    {
        [Required]
        [StringLength(25, MinimumLength = 2)]
        [DisplayName("Fornavn")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(40, MinimumLength = 2)]
        [DisplayName("Efternavn")]
        public string LastName { get; set; }

        [DisplayName("Studerende")]
        public bool IsStudent { get; set; }

        [DisplayName("Ansat")]
        public bool IsEmployee { get; set; }

        // Perhaps change this to a guid instead of int
        public int? StudentNum { get; set; }

        // Perhaps change this to a guid instead of int
        public int? EmployeeNum { get; set; }

    }
}
