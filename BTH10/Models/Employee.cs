using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTH7.Models
{
    [Table("Employees")]

    public class Employee
    {
        [Key]
        public string Id { get; set; }

        public string Name { get; set; }

        [EmailAddress(ErrorMessage = "Vui lòng nhập đúng định dạng email")]
        public string Email { get; set; }

    }
}