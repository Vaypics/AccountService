using AccountService.DTOs;
using AccountService.Models;

namespace AccountService.Interfaces
{
    public interface IAccountRepository
    {
        Account Create(CreateAccountDto dto);
        List<Account> GetAll();
        Account? GetById(Guid id);
        List<Account> GetByOwnerId(Guid ownerId);
        void Update(Guid id, UpdateAccountDto dto);
        void UpdateFull(Guid id, UpdateAccountFullDto dto);
        void Delete(Guid id);
        bool AccountExists(Guid accountId, Guid ownerId);
        void RegisterTransaction(TransactionDto dto);
        void Transfer(TransferDto dto);
        List<Transaction> GetStatement(StatementRequestDto dto);
    }
}