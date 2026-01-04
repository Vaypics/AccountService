using AccountService.Models;

namespace AccountService.DTOs
{
    public class TransactionResponseDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid? CounterpartyAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public TransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }
}