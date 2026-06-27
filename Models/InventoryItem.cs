using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class InventoryItem
{
    [Key]
    public int ItemId { get; set; }

    public required string Name { get; set; }
    public int Quantity { get; set; }
    public required string Location { get; set; }

    public int? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    public void DisplayInfo()
    {
        System.Console.WriteLine($"Item: {Name} | Quantity: {Quantity} | Location: {Location}");
    }
}