using System.ComponentModel.DataAnnotations;

namespace itpayroll.Models
{
    public class TaxBracket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Min Amount")]
        public decimal MinAmount { get; set; }

        [Display(Name = "Max Amount")]
        public decimal? MaxAmount { get; set; }

        [Required]
        [Display(Name = "Base Tax")]
        public decimal BaseTax { get; set; }

        [Required]
        [Display(Name = "Tax Rate")]
        [Range(0, 1)]
        public decimal TaxRate { get; set; }

        [Required]
        public int SortOrder { get; set; }

        [Display(Name = "Year")]
        public int Year { get; set; } = DateTime.UtcNow.Year;
    }
}
