using System.ComponentModel.DataAnnotations;
using AccountService.Models;

namespace AccountService.DTOs
{
    public class TransactionDto
    {
        [Required(ErrorMessage = "AccountId обязателен")]
        public Guid AccountId { get; set; }

        public Guid? CounterpartyAccountId { get; set; }

        [Required(ErrorMessage = "Сумма обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
        public decimal Amount { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Код валюты должен содержать 3 символа")]
        public string Currency { get; set; } = "RUB";

        [Required(ErrorMessage = "Тип транзакции обязателен")]
        public TransactionType Type { get; set; }

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string Description { get; set; } = string.Empty;
    }
}