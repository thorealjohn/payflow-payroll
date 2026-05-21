using System.ComponentModel.DataAnnotations;

namespace itpayroll.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Position> Positions { get; set; } = new List<Position>();
    }
}
