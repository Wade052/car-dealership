namespace car_dealership.Models;

// The example rate is the annual interest rate. Demo calculations assume zero fees.
public sealed record PurchaseQuote(int CarId, string BuyerId, decimal Price,
    decimal DownPayment, int Months, decimal AnnualRate)
{
    public decimal AmountFinanced => Price - DownPayment;

    private decimal UnroundedPayment
    {
        get
        {
            if (AnnualRate == 0) return AmountFinanced / Months;

            var monthlyRate = AnnualRate / 1200m;
            var factor = 1 - (decimal)Math.Pow(1 + (double)monthlyRate, -Months);
            return AmountFinanced * monthlyRate / factor;
        }
    }

    public decimal MonthlyPayment => decimal.Round(UnroundedPayment, 2, MidpointRounding.AwayFromZero);
    public decimal TotalCost => decimal.Round(DownPayment + UnroundedPayment * Months, 2, MidpointRounding.AwayFromZero);
    public decimal TotalInterest => TotalCost - Price;

    // Invented rates for the demo. These thresholds are not lender criteria.
    public static decimal ExampleRate(int score) => score switch
    {
        >= 760 => 5m,
        >= 700 => 7m,
        >= 640 => 10m,
        _ => 14m
    };
}
