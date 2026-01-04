using System.ComponentModel.DataAnnotations;

namespace AccountService.DTOs
{
    public class UpdateAccountDto
    {
        [Range(0, 100, ErrorMessage = "Процентная ставка должна быть от 0 до 100")]
        public decimal? InterestRate { get; set; }
        public DateTime? ClosedDate { get; set; }
    }
}