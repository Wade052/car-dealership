using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace car_dealership.Models;

public sealed class BuyCarInput
{
    [BindNever, ValidateNever]
    public Car Vehicle { get; set; } = null!;

    [Required, Range(300, 850), Display(Name = "Example credit score")]
    public int? CreditScore { get; set; }

    [Required, Range(typeof(decimal), "0", "1000000000"), Display(Name = "Down payment")]
    public decimal? DownPayment { get; set; }

    [BindRequired, Display(Name = "Loan term")]
    public int Months { get; set; } = 60;

    [BindNever]
    public PurchaseQuote? Estimate { get; set; }

    [BindNever]
    public string? ConfirmationToken { get; set; }
}
