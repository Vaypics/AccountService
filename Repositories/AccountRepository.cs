using Microsoft.EntityFrameworkCore;
using AccountService.DTOs;
using AccountService.Interfaces;
using AccountService.Models;
using AccountService.Data;

namespace AccountService.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(
            ApplicationDbContext context,
            ILogger<AccountRepository> logger)
        {
            _context = context;
            _logger = logger;
        }


        public async Task<Account> Create(CreateAccountDto dto)
        {
            if (dto.Type == null)
                throw new ArgumentException("Тип счета обязателен");

            if (string.IsNullOrWhiteSpace(dto.Currency))
                throw new ArgumentException("Валюта обязательна");

            var account = new Account
            {
                Id = Guid.NewGuid(),
                OwnerId = dto.OwnerId,
                Type = dto.Type.Value,  
                Currency = dto.Currency,
                Balance = 0,
                InterestRate = dto.InterestRate,
                OpenedDate = DateTime.UtcNow,
                ClosedDate = null,
                Transactions = new List<Transaction>()
            };

            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Создан новый счет {AccountId} для клиента {OwnerId}",
                account.Id, account.OwnerId);

            return account;
        }

        public async Task<List<Account>> GetAll()
        {

            return await _context.Accounts
                .Include(a => a.Transactions) 
                .ToListAsync();
        }

        public async Task<Account?> GetById(Guid id)
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Account>> GetByOwnerId(Guid ownerId)
        {
            return await _context.Accounts
                .Where(a => a.OwnerId == ownerId)
                .Include(a => a.Transactions)
                .ToListAsync();
        }

        public async Task Update(Guid id, UpdateAccountDto dto)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {id} не найден");

            if (dto.InterestRate.HasValue)
                account.InterestRate = dto.InterestRate.Value;

            if (dto.ClosedDate.HasValue)
                account.ClosedDate = dto.ClosedDate.Value;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Обновлен счет {AccountId}", id);
        }

        public async Task UpdateFull(Guid id, UpdateAccountFullDto dto)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {id} не найден");

            account.OwnerId = dto.OwnerId;
            account.Type = dto.Type;
            account.Currency = dto.Currency;
            account.Balance = dto.Balance;
            account.InterestRate = dto.InterestRate;
            account.OpenedDate = dto.OpenedDate;
            account.ClosedDate = dto.ClosedDate;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Полное обновление счета {AccountId}", id);
        }

        public async Task Delete(Guid id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {id} не найден");

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Удален счет {AccountId}", id);
        }


        public async Task<bool> AccountExists(Guid accountId, Guid ownerId)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Id == accountId && a.OwnerId == ownerId);
        }

        public async Task RegisterTransaction(TransactionDto dto)
        {
            var account = await _context.Accounts.FindAsync(dto.AccountId);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {dto.AccountId} не найден");

            if (dto.Type == TransactionType.Debit && account.Balance < dto.Amount)
                throw new InvalidOperationException(
                    $"Недостаточно средств. Баланс: {account.Balance}, требуется: {dto.Amount}");

            var transaction = new Transaction
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

            if (dto.Type == TransactionType.Credit)
                account.Balance += dto.Amount;
            else
                account.Balance -= dto.Amount;

            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Транзакция {TransactionId} зарегистрирована для счета {AccountId}",
                transaction.Id, account.Id);
        }

        public async Task Transfer(TransferDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var fromAccount = await _context.Accounts.FindAsync(dto.FromAccountId);
                var toAccount = await _context.Accounts.FindAsync(dto.ToAccountId);

                if (fromAccount == null)
                    throw new KeyNotFoundException($"Счет-отправитель {dto.FromAccountId} не найден");

                if (toAccount == null)
                    throw new KeyNotFoundException($"Счет-получатель {dto.ToAccountId} не найден");

                if (fromAccount.Balance < dto.Amount)
                    throw new InvalidOperationException(
                        $"Недостаточно средств. Баланс: {fromAccount.Balance}, требуется: {dto.Amount}");

                if (fromAccount.Currency != toAccount.Currency)
                    throw new InvalidOperationException(
                        $"Валюты не совпадают: {fromAccount.Currency} != {toAccount.Currency}");

                var debitTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = dto.FromAccountId,
                    CounterpartyAccountId = dto.ToAccountId,
                    Amount = dto.Amount,
                    Currency = fromAccount.Currency,
                    Type = TransactionType.Debit,
                    Description = $"Перевод на счет {dto.ToAccountId}: {dto.Description}",
                    TransactionDate = DateTime.UtcNow
                };

                var creditTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = dto.ToAccountId,
                    CounterpartyAccountId = dto.FromAccountId,
                    Amount = dto.Amount,
                    Currency = toAccount.Currency,
                    Type = TransactionType.Credit,
                    Description = $"Поступление со счета {dto.FromAccountId}: {dto.Description}",
                    TransactionDate = DateTime.UtcNow
                };

                fromAccount.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;

                await _context.Transactions.AddAsync(debitTransaction);
                await _context.Transactions.AddAsync(creditTransaction);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Перевод {Amount} со счета {From} на счет {To} выполнен",
                    dto.Amount, dto.FromAccountId, dto.ToAccountId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Transaction>> GetStatement(StatementRequestDto dto)
        {
            if (dto.FromDate > dto.ToDate)
                throw new ArgumentException("Начальная дата не может быть позже конечной");

            if (!await _context.Accounts.AnyAsync(a => a.Id == dto.AccountId))
                throw new KeyNotFoundException($"Счет с ID {dto.AccountId} не найден");

            return await _context.Transactions
                .Where(t => t.AccountId == dto.AccountId)
                .Where(t => t.TransactionDate >= dto.FromDate && t.TransactionDate <= dto.ToDate)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task InitializeTestData()
        {
            if (await _context.Accounts.AnyAsync())
                return;

            var ivanId = Guid.NewGuid();
            _logger.LogInformation("Создаем тестовые данные для клиента {OwnerId}", ivanId);

            var checkingAccount = new Account
            {
                Id = Guid.NewGuid(),
                OwnerId = ivanId,
                Type = AccountType.Checking,
                Currency = "RUB",
                Balance = 1000,
                OpenedDate = DateTime.UtcNow.AddDays(-30),
                ClosedDate = null,
                Transactions = new List<Transaction>()
            };

            var depositAccount = new Account
            {
                Id = Guid.NewGuid(),
                OwnerId = ivanId,
                Type = AccountType.Deposit,
                Currency = "RUB",
                Balance = 200,
                InterestRate = 3.0m,
                OpenedDate = DateTime.UtcNow.AddDays(-30),
                ClosedDate = null,
                Transactions = new List<Transaction>()
            };

            await _context.Accounts.AddRangeAsync(checkingAccount, depositAccount);
            await _context.SaveChangesAsync();

            var transactions = new[]
            {
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = checkingAccount.Id,
                    Amount = 1000,
                    Currency = "RUB",
                    Type = TransactionType.Credit,
                    Description = "Пополнение наличными кассиром Алексеем",
                    TransactionDate = DateTime.UtcNow.AddDays(-20)
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = checkingAccount.Id,
                    CounterpartyAccountId = depositAccount.Id,
                    Amount = 200,
                    Currency = "RUB",
                    Type = TransactionType.Debit,
                    Description = "Перевод на вклад 'Надёжный-6'",
                    TransactionDate = DateTime.UtcNow.AddDays(-15)
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = depositAccount.Id,
                    CounterpartyAccountId = checkingAccount.Id,
                    Amount = 200,
                    Currency = "RUB",
                    Type = TransactionType.Credit,
                    Description = "Поступление с текущего счета",
                    TransactionDate = DateTime.UtcNow.AddDays(-15)
                }
            };

            await _context.Transactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Тестовые данные успешно созданы");
        }
    }
}