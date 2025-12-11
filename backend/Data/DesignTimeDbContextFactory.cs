using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RechnungsfreigabeAPI.Data;

namespace RechnungsfreigabeAPI.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use default connection string for design-time operations
        var connectionString = "Server=localhost;Database=rechnungsfreigabe;User=root;Password=;";
        
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}