using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Website_Mua_Ban_Rao_Vat.Models
{
    public class ListingModel
    {
        public Listing Listing { get; set; }
        public List<Listing> RelateListing { get; set; }
        public User User { get; set; }
        public List<Image> Images { get; set; }
        public List<Rating> Rating { get; set; }
    }
}