using Microsoft.EntityFrameworkCore;
using MultipleOffsiteDownloader.Models;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<Login> Logins { get; set; }

    public bool IsDatabaseConnected()
    {
        try
        {
            return Database.CanConnect();
        }
        catch
        {
            return false;
        }
    }


}
