using System.ComponentModel.DataAnnotations;

namespace AccountService.DTOs
{
    public class TransferDto
    {
        [Required(ErrorMessage = "FromAccountId обязателен")]
        public Guid FromAccountId { get; set; }
        [Required(ErrorMessage = "ToAccountId обязателен")]
        public Guid ToAccountId { get; set; }
        [Required(ErrorMessage = "Сумма обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
        public decimal Amount { get; set; }
        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string Description { get; set; } = string.Empty;
    }
}