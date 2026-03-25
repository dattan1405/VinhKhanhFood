using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinhKhanhFood.App.Models
{
    public class FoodLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? AudioUrl { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}
