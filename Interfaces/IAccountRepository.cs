using AccountService.DTOs;
using AccountService.Models;

namespace AccountService.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account> Create(CreateAccountDto dto);
        Task<List<Account>> GetAll();
        Task<Account?> GetById(Guid id);
        Task<List<Account>> GetByOwnerId(Guid ownerId);
        Task Update(Guid id, UpdateAccountDto dto);
        Task UpdateFull(Guid id, UpdateAccountFullDto dto);
        Task Delete(Guid id);
        Task<bool> AccountExists(Guid accountId, Guid ownerId);
        Task RegisterTransaction(TransactionDto dto);
        Task Transfer(TransferDto dto);
        Task<List<Transaction>> GetStatement(StatementRequestDto dto);
        Task InitializeTestData();
    }
}