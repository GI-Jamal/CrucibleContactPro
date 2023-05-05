using System.ComponentModel.DataAnnotations;

namespace CrucibleContactPro.Models
{
    public class EmailData
    {
        [Required]
        public string? EmailAddress { get; set; }
        [Required]
        [Display(Name = "Email Body")]
        public string? EmailBody { get; set; }
        [Required]
        [Display(Name = "Email Subject")]
        public string? EmailSubject { get; set;}

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? GroupName { get; set; }
    }
}
