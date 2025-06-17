using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Website_Mua_Ban_Rao_Vat.Controllers;

namespace Website_Mua_Ban_Rao_Vat.Models
{
    public class HomeModel
    {
        public List<Banner> Bans { get; set; }
        public List<Category> Categories { get; set; }
        public List<Listing> Listing { get; set; }
        public virtual ICollection<Image> Images { get; set; }
        public List<Rating> Rating { get; set; }
        public Dictionary<int, List<Rating>> RatingByListing { get; set; }
        public Dictionary<int, decimal> UserRatingAvg { get; set; }

    }
}