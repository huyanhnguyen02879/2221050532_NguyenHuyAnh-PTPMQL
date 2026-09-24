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

        
        public string Email { get; set; }

    }
}