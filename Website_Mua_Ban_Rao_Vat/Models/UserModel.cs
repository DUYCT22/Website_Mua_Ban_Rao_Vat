using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Website_Mua_Ban_Rao_Vat.Models
{
    public class UserModel
    {
        public User user {  get; set; }
        public List<Listing> listingShowing{ get; set; }
        public List<Listing> listingPendingApproval{ get; set; }
        public List<Listing> listingSold{ get; set; }
        public List<Follow> follows{ get; set; }
        public List<Rating> Rating { get; set; }
    }
}