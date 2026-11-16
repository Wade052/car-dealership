using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace car_dealership.Models;

public sealed class CarPurchase
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
    public string? BuyerId { get; set; }
    public IdentityUser? Buyer { get; set; }
    public DateTime PurchasedAtUtc { get; set; }
    public decimal Price { get; set; }
    public decimal DownPayment { get; set; }
    public int Months { get; set; }
    public decimal AnnualRate { get; set; }

    [NotMapped]
    public PurchaseQuote Estimate => new(CarId, BuyerId ?? "", Price, DownPayment, Months, AnnualRate);
}
