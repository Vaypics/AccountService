using AccountService.Models;

namespace AccountService.DTOs
{
    public class AccountResponseDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public AccountType Type { get; set; }
        public string Currency { get; set; } = "RUB";
        public decimal Balance { get; set; }
        public decimal? InterestRate { get; set; }
        public DateTime OpenedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
    }
}