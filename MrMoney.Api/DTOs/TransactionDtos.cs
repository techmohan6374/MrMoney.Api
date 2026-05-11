using System.ComponentModel.DataAnnotations;

namespace MrMoney.Api.DTOs
{
    public class CreateTransactionRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [RegularExpression("^(income|expense|transfer)$", ErrorMessage = "Type must be income, expense, or transfer.")]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string AccountId { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime? Date { get; set; }
    }

    public class UpdateTransactionRequest
    {
        [StringLength(200, MinimumLength = 1)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; }

        [RegularExpression("^(income|expense|transfer)$")]
        public string? Type { get; set; }

        public string? AccountId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime? Date { get; set; }
    }

    public class TransferRequest
    {
        [Required]
        public string FromAccountId { get; set; } = string.Empty;

        [Required]
        public string ToAccountId { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public DateTime? Date { get; set; }
    }

    public class TransactionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TransactionFilterRequest
    {
        public string? Type { get; set; }           // income | expense | transfer | All
        public string? AccountId { get; set; }
        public string? Category { get; set; }
        public string? Search { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class PagedTransactionResponse
    {
        public List<TransactionResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
