using System.ComponentModel.DataAnnotations;

namespace AccountService.DTOs
{
    public class StatementRequestDto
    {
        [Required(ErrorMessage = "AccountId обязателен")]
        public Guid AccountId { get; set; }

        [Required(ErrorMessage = "Начальная дата обязательна")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "Конечная дата обязательна")]
        public DateTime ToDate { get; set; }
    }
}