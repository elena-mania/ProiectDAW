using System.ComponentModel.DataAnnotations;

namespace ProiectDAW.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul este obligatoriu")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int? TopicId { get; set; }

        public virtual Topic? Topic { get; set; }
        //un comentariu apartine unui utilizator (fk)
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

    }
}
