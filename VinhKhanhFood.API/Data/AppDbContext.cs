using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Models;

namespace VinhKhanhFood.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FoodLocation> FoodLocations { get; set; }

        public DbSet<User> Users { get; set; }
    }
}