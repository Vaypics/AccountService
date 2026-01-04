using AccountService.Models;

namespace AccountService.DTOs
{
    public static class MappingExtensions
    {
        public static AccountResponseDto ToResponseDto(this Account account)
        {
            return new AccountResponseDto
            {
                Id = account.Id,
                OwnerId = account.OwnerId,
                Type = account.Type,
                Currency = account.Currency,
                Balance = account.Balance,
                InterestRate = account.InterestRate,
                OpenedDate = account.OpenedDate,
                ClosedDate = account.ClosedDate
            };
        }

        public static TransactionResponseDto ToResponseDto(this Transaction transaction)
        {
            return new TransactionResponseDto
            {
                Id = transaction.Id,
                AccountId = transaction.AccountId,
                CounterpartyAccountId = transaction.CounterpartyAccountId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Type = transaction.Type,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate
            };
        }

        public static Transaction ToModel(this TransactionDto dto)
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                CounterpartyAccountId = dto.CounterpartyAccountId,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Type = dto.Type,
                Description = dto.Description,
                TransactionDate = DateTime.UtcNow
            };
        }
    }
}