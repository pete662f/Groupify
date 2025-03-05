using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Groupify.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(25, MinimumLength = 2)]
        [DisplayName("Fornavn")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(40, MinimumLength = 2)]
        [DisplayName("Efternavn")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        // MORE STUFF TO COME
    }
}
