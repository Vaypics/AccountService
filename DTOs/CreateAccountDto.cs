using System.ComponentModel.DataAnnotations;
using AccountService.Models;

namespace AccountService.DTOs
{
    public class CreateAccountDto
    {
        [Required(ErrorMessage = "OwnerId обязателен")]
        public Guid OwnerId { get; set; }

        [Required(ErrorMessage = "Тип счета обязателен")]
        [Range(0, 2, ErrorMessage = "Тип счета должен быть: 0-Checking, 1-Deposit, 2-Credit")]
        public AccountType Type { get; set; }

        [Required(ErrorMessage = "Валюта обязательна")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Код валюты должен содержать 3 символа")]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Неверный формат валюты. Пример: RUB, USD, EUR")]
        public string Currency { get; set; } = "RUB";

        [Range(0, 100, ErrorMessage = "Процентная ставка должна быть от 0 до 100")]
        public decimal? InterestRate { get; set; }
    }
}