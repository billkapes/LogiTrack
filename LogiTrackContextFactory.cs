using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class LogiTrackContextFactory : IDesignTimeDbContextFactory<LogiTrackContext>
{
    public LogiTrackContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LogiTrackContext>();
        optionsBuilder.UseSqlite("Data Source=logitrack.db");
        return new LogiTrackContext(optionsBuilder.Options);
    }
}