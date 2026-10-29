using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTH7.Models
{
    [Table("Students")]

    public class Student
    {
        [Key]
        public int Id { get; set; }

        
        public string Name { get; set; }

        [EmailAddress(ErrorMessage = "Vui lòng nhập đúng định dạng email")]
        public string Email { get; set; }

        public int FacultyId { get; set; }

        public Faculty? Faculty { get; set; }

        public string StudentCode { get; set; } = "";

    }
}