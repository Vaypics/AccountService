using AccountService.DTOs;
using AccountService.Interfaces;
using AccountService.Models;

namespace AccountService.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();
        private readonly List<Transaction> _transactions = new();
        public AccountRepository()
        {
            InitializeTestData();
        }
        private void InitializeTestData()
        {
            var ivanId = Guid.NewGuid();
            Console.WriteLine($"Создан тестовый клиент Иван с ID: {ivanId}");

            var checkingAccount = new Account
            {
                Id = Guid.NewGuid(),
                OwnerId = ivanId,
                Type = AccountType.Checking,
                Currency = "RUB",
                Balance = 1000,
                OpenedDate = DateTime.UtcNow.AddDays(-30),
                ClosedDate = null
            };
            _accounts.Add(checkingAccount);
            Console.WriteLine($"Создан текущий счет: {checkingAccount.Id}");

            var depositAccount = new Account
            {
                Id = Guid.NewGuid(),
                OwnerId = ivanId,
                Type = AccountType.Deposit,
                Currency = "RUB",
                Balance = 200,
                InterestRate = 3.0m,
                OpenedDate = DateTime.UtcNow.AddDays(-30),
                ClosedDate = null
            };
            _accounts.Add(depositAccount);
            Console.WriteLine($"Создан вклад: {depositAccount.Id}");
            CreateTestTransactions(checkingAccount.Id, depositAccount.Id, ivanId);
        }

        private void CreateTestTransactions(Guid checkingId, Guid depositId, Guid ownerId)
        {
            var transaction1 = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = checkingId,
                CounterpartyAccountId = null,
                Amount = 1000,
                Currency = "RUB",
                Type = TransactionType.Credit,
                Description = "Пополнение наличными кассиром Алексеем",
                TransactionDate = DateTime.UtcNow.AddDays(-20)
            };
            _transactions.Add(transaction1);

            var transaction2 = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = checkingId,
                CounterpartyAccountId = depositId,
                Amount = 200,
                Currency = "RUB",
                Type = TransactionType.Debit,
                Description = "Перевод на вклад 'Надёжный-6'",
                TransactionDate = DateTime.UtcNow.AddDays(-15)
            };
            _transactions.Add(transaction2);

            var transaction3 = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = depositId,
                CounterpartyAccountId = checkingId,
                Amount = 200,
                Currency = "RUB",
                Type = TransactionType.Credit,
                Description = "Поступление с текущего счета",
                TransactionDate = DateTime.UtcNow.AddDays(-15)
            };
            _transactions.Add(transaction3);

            var checkingAccount = _accounts.First(a => a.Id == checkingId);
            checkingAccount.Transactions.Add(transaction1);
            checkingAccount.Transactions.Add(transaction2);

            var depositAccount = _accounts.First(a => a.Id == depositId);
            depositAccount.Transactions.Add(transaction3);
        }
        public Account Create(CreateAccountDto dto)
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                OwnerId = dto.OwnerId,
                Type = dto.Type,
                Currency = dto.Currency,
                Balance = 0,
                InterestRate = dto.InterestRate,
                OpenedDate = DateTime.UtcNow,
                ClosedDate = null,
                Transactions = new List<Transaction>()
            };
            _accounts.Add(account);
            Console.WriteLine($"Создан новый счет: {account.Id} для клиента: {account.OwnerId}");

            return account;
        }
        public List<Account> GetAll()
        {
            return _accounts;
        }
        public Account? GetById(Guid id)
        {
            return _accounts.FirstOrDefault(a => a.Id == id);
        }
        public List<Account> GetByOwnerId(Guid ownerId)
        {
            return _accounts.Where(a => a.OwnerId == ownerId).ToList();
        }
        public void Update(Guid id, UpdateAccountDto dto)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == id);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {id} не найден");
            if (dto.InterestRate.HasValue)
                account.InterestRate = dto.InterestRate.Value;

            if (dto.ClosedDate.HasValue)
                account.ClosedDate = dto.ClosedDate.Value;

            Console.WriteLine($"Обновлен счет: {id}");
        }
        public void Delete(Guid id)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == id);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {id} не найден");

            _accounts.Remove(account);
            Console.WriteLine($"Удален счет: {id}");
        }

        public bool AccountExists(Guid accountId, Guid ownerId)
        {
            return _accounts.Any(a => a.Id == accountId && a.OwnerId == ownerId);
        }

        public void RegisterTransaction(TransactionDto dto)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == dto.AccountId);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {dto.AccountId} не найден");

            if (dto.Type == TransactionType.Debit && account.Balance < dto.Amount)
                throw new InvalidOperationException($"Недостаточно средств на счете. Баланс: {account.Balance}, требуется: {dto.Amount}");

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
            _transactions.Add(transaction);
            account.Transactions.Add(transaction);

            Console.WriteLine($"Зарегистрирована транзакция: {transaction.Id} на сумму {dto.Amount} {dto.Currency}");
        }

        public void Transfer(TransferDto dto)
        {
            var fromAccount = _accounts.FirstOrDefault(a => a.Id == dto.FromAccountId);
            var toAccount = _accounts.FirstOrDefault(a => a.Id == dto.ToAccountId);
            if (fromAccount == null)
                throw new KeyNotFoundException($"Счет-отправитель с ID {dto.FromAccountId} не найден");
            if (toAccount == null)
                throw new KeyNotFoundException($"Счет-получатель с ID {dto.ToAccountId} не найден");

            if (fromAccount.Balance < dto.Amount)
                throw new InvalidOperationException($"Недостаточно средств на счете-отправителе. Баланс: {fromAccount.Balance}, требуется: {dto.Amount}");

            if (fromAccount.Currency != toAccount.Currency)
                throw new InvalidOperationException($"Валюты счетов не совпадают: {fromAccount.Currency} != {toAccount.Currency}");

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
            _transactions.Add(debitTransaction);
            _transactions.Add(creditTransaction);
            fromAccount.Transactions.Add(debitTransaction);
            toAccount.Transactions.Add(creditTransaction);

            Console.WriteLine($"Выполнен перевод: {dto.FromAccountId} -> {dto.ToAccountId} на сумму {dto.Amount} {fromAccount.Currency}");
        }

        public List<Transaction> GetStatement(StatementRequestDto dto)
        {
            if (dto.FromDate > dto.ToDate)
                throw new ArgumentException("Начальная дата не может быть позже конечной даты");
            if (!_accounts.Any(a => a.Id == dto.AccountId))
                throw new KeyNotFoundException($"Счет с ID {dto.AccountId} не найден");
            var statement = _transactions
                .Where(t => t.AccountId == dto.AccountId)
                .Where(t => t.TransactionDate >= dto.FromDate && t.TransactionDate <= dto.ToDate)
                .OrderBy(t => t.TransactionDate)
                .ToList();

            Console.WriteLine($"Сформирована выписка по счету {dto.AccountId} за период {dto.FromDate:dd.MM.yyyy} - {dto.ToDate:dd.MM.yyyy}. Найдено транзакций: {statement.Count}");

            return statement;
        }
        public void UpdateFull(Guid id, UpdateAccountFullDto dto)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == id);
            if (account == null)
                throw new KeyNotFoundException($"Счет с ID {id} не найден");

            account.OwnerId = dto.OwnerId;
            account.Type = dto.Type;
            account.Currency = dto.Currency;
            account.Balance = dto.Balance;
            account.InterestRate = dto.InterestRate;
            account.OpenedDate = dto.OpenedDate;
            account.ClosedDate = dto.ClosedDate;
        }
    }
}
