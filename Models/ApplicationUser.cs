using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProiectDAW.Models
{
    public class ApplicationUser : IdentityUser
    {

        //un user posteaza mai multe mesaje
        public virtual ICollection<Comment>? Comments { get; set; }
        //un user poate initia mai multe subiecte de discutie
        public virtual ICollection<Topic>? Topics { get; set; }
        // atribute suplimentare adaugate pentru user
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        // variabila in care vom retine rolurile existente in baza de date
        // pentru popularea unui dropdown list
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}
