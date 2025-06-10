using HousePredictionAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePredictionAPI;

public class HousePredictionDBContext : DbContext
{
    
    public HousePredictionDBContext(DbContextOptions<HousePredictionDBContext> options)
        : base(options) { }

    public DbSet<HouseDetails> HouseDetails => Set<HouseDetails>();
}