using System.ComponentModel.DataAnnotations.Schema;

namespace HousePredictionAPI.Entities;

public class HouseDetails
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Neighborhood { get; set; }
    public int YearBuilt { get; set; }
    public int TotalBsmtSF { get; set; }
    public int GrLivArea { get; set; }
    public int OverallQual { get; set; }
    public int FullBath { get; set; }
    public int TotRmsAbvGrd { get; set; }
    public int GarageArea { get; set; }
    public decimal? SalePrice { get; set; }
    
}