using System;
using System.Collections.Generic;

public class Order
{
    public int OrderId { get; set; }
    public required string CustomerName { get; set; }
    public DateTime DatePlaced { get; set; }

    public List<InventoryItem> Items { get; set; } = new();

    public void AddItem(InventoryItem item)
    {
        Items.Add(item);
    }

    public void RemoveItem(int itemId)
    {
        var index = Items.FindIndex(i => i.ItemId == itemId);
        if (index >= 0) Items.RemoveAt(index);
    }

    public string GetOrderSummary()
    {
        return $"Order #{OrderId} for {CustomerName} | Items: {Items.Count} | Placed: {DatePlaced:M/d/yyyy}";
    }
}
