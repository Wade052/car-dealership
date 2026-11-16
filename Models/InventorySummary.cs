namespace car_dealership.Models;

public sealed record InventorySummary(
    int TotalListings, int Sold, int Pending, int Reserved, int Available, decimal InventoryValue)
{
    public int Unsold => TotalListings - Sold;
}
