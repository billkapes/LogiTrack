using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class LogiTrackContext : DbContext
{
    public LogiTrackContext(DbContextOptions<LogiTrackContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=logitrack.db");
}