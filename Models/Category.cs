using System.ComponentModel.DataAnnotations;

namespace CrucibleContactPro.Models
{
    public class Category
    {
        public int Id { get; set; }

        // Foreign Key
        [Required]
        public string? AppUserId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string? CategoryName { get; set; }

        // Navigation Properties
        public virtual AppUser? AppUser { get; set; } // one to many relationship
        public virtual ICollection<Contact> Contacts { get; set; } = new HashSet<Contact>(); // many to many relationship
    }
}
