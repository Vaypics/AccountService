using Microsoft.AspNetCore.Mvc;
using AccountService.DTOs;
using AccountService.Interfaces;
using AccountService.Models;
using AccountService.Repositories;

namespace AccountService.Controlllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountRepository _repository;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(
            IAccountRepository repository,
            ILogger<AccountsController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Контроллер AccountsController инициализирован");
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AccountResponseDto>), StatusCodes.Status200OK)]
        public IActionResult GetAllAccounts()
        {
            _logger.LogInformation("Запрос на получение всех счетов");

            var accounts = _repository.GetAll();
            var response = accounts.Select(a => a.ToResponseDto()).ToList();

            _logger.LogInformation("Найдено {Count} счетов", response.Count);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetAccount(Guid id)
        {
            _logger.LogInformation("Запрос на получение счета {AccountId}", id);

            var account = _repository.GetById(id);
            if (account == null)
            {
                _logger.LogWarning("Счет {AccountId} не найден", id);
                return NotFound($"Счет с ID {id} не найден");
            }

            var response = account.ToResponseDto();
            return Ok(response);
        }

        [HttpGet("owner/{ownerId:guid}")]
        [ProducesResponseType(typeof(List<AccountResponseDto>), StatusCodes.Status200OK)]
        public IActionResult GetAccountsByOwner(Guid ownerId)
        {
            _logger.LogInformation("Запрос на получение счетов клиента {OwnerId}", ownerId);

            var accounts = _repository.GetByOwnerId(ownerId);
            var response = accounts.Select(a => a.ToResponseDto()).ToList();

            _logger.LogInformation("Найдено {Count} счетов для клиента {OwnerId}", response.Count, ownerId);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(AccountResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult CreateAccount([FromBody] CreateAccountDto dto)
        {
            _logger.LogInformation("Запрос на создание нового счета для клиента {OwnerId}", dto.OwnerId);

            if (dto == null || dto.OwnerId == Guid.Empty)
            {
                return BadRequest("Тело запроса не может быть пустым. Обязательные поля: ownerId, type, currency");
            }

            if ((dto.Type == AccountType.Deposit || dto.Type == AccountType.Credit)
       && !dto.InterestRate.HasValue)
            {
                return BadRequest("Для вкладов и кредитных счетов процентная ставка обязательна");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Некорректные данные при создании счета: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var account = _repository.Create(dto);
                var response = account.ToResponseDto();

                _logger.LogInformation("Создан новый счет {AccountId} типа {AccountType}", account.Id, account.Type);

                return CreatedAtAction(
                    nameof(GetAccount),
                    new { id = account.Id },
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании счета");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdateAccount(Guid id, [FromBody] UpdateAccountDto dto)
        {
            _logger.LogInformation("Запрос на обновление счета {AccountId}", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Некорректные данные при обновлении счета {AccountId}: {@ModelState}", id, ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                _repository.Update(id, dto);
                _logger.LogInformation("Счет {AccountId} успешно обновлен", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Счет {AccountId} не найден при обновлении", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении счета {AccountId}", id);
                return BadRequest(ex.Message);
            }
        }
 
        [HttpPut("{id:guid}/full")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdateAccountFull(Guid id, [FromBody] UpdateAccountFullDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Тело запроса не может быть пустым");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _repository.UpdateFull(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteAccount(Guid id)
        {
            _logger.LogInformation("Запрос на удаление счета {AccountId}", id);

            try
            {
                _repository.Delete(id);
                _logger.LogInformation("Счет {AccountId} успешно удален", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Счет {AccountId} не найден при удалении", id);
                return NotFound(ex.Message);
            }
        }

        [HttpGet("exists/{accountId:guid}/{ownerId:guid}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public IActionResult CheckAccountExists(Guid accountId, Guid ownerId)
        {
            _logger.LogInformation("Проверка существования счета {AccountId} у клиента {OwnerId}", accountId, ownerId);

            var exists = _repository.AccountExists(accountId, ownerId);

            _logger.LogInformation("Счет {AccountId} {Status} у клиента {OwnerId}",
                accountId, exists ? "существует" : "не существует", ownerId);

            return Ok(exists);
        }

        [HttpPost("transactions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult RegisterTransaction([FromBody] TransactionDto dto)
        {
            _logger.LogInformation("Запрос на регистрацию транзакции по счету {AccountId} на сумму {Amount} {Currency}",
                dto.AccountId, dto.Amount, dto.Currency);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Некорректные данные транзакции: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                _repository.RegisterTransaction(dto);
                _logger.LogInformation("Транзакция успешно зарегистрирована для счета {AccountId}", dto.AccountId);
                return Ok(new { Message = "Транзакция успешно зарегистрирована" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Счет {AccountId} не найден при регистрации транзакции", dto.AccountId);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ошибка бизнес-логики при регистрации транзакции");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неизвестная ошибка при регистрации транзакции");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("transfer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Transfer([FromBody] TransferDto dto)
        {
            _logger.LogInformation("Запрос на перевод {Amount} с {FromAccountId} на {ToAccountId}",
                dto.Amount, dto.FromAccountId, dto.ToAccountId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Некорректные данные перевода: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                _repository.Transfer(dto);
                _logger.LogInformation("Перевод успешно выполнен: {FromAccountId} -> {ToAccountId} на сумму {Amount}",
                    dto.FromAccountId, dto.ToAccountId, dto.Amount);

                return Ok(new { Message = "Перевод успешно выполнен" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Счет не найден при переводе");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ошибка бизнес-логики при переводе");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неизвестная ошибка при переводе");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("statement")]
        [ProducesResponseType(typeof(List<TransactionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetStatement([FromBody] StatementRequestDto dto)
        {
            _logger.LogInformation("Запрос выписки по счету {AccountId} за период {FromDate} - {ToDate}",
                dto.AccountId, dto.FromDate, dto.ToDate);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Некорректные параметры запроса выписки: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var transactions = _repository.GetStatement(dto);
                var response = transactions.Select(t => t.ToResponseDto()).ToList();

                _logger.LogInformation("Сформирована выписка для счета {AccountId}: {Count} транзакций",
                    dto.AccountId, response.Count);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Некорректные даты в запросе выписки");
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Счет {AccountId} не найден при запросе выписки", dto.AccountId);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("version")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetVersion()
        {
            var versionInfo = new
            {
                Service = "Account Service",
                Version = "1.0.0",
                Description = "Микросервис для управления банковскими счетами",
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Запрос информации о версии API");
            return Ok(versionInfo);
        }
    }
}