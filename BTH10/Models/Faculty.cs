using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTH7.Models
{
    [Table("Faculties")]
    public class Faculty
    {
        [Key]
        public int FacultyId { get; set; }

        [Required]
        public string FacultyCode { get; set; } = "";

        [Required]
        public string FacultyName { get; set; } = "";

        public ICollection<Student>? Students { get; set; }
    }
}