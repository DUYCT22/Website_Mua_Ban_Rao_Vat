using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class CloudinaryService
    {
        private Cloudinary cloudinary;

        public CloudinaryService()
        {
            Account account = new Account(
                "dgpw5aart",
                "613462792535466",
                "iYTtVAxtts6GBXvuGIpZHEh8wmQ"
            );
            cloudinary = new Cloudinary(account);
        }

        public ImageUploadResult Upload(ImageUploadParams uploadParams)
        {
            return cloudinary.Upload(uploadParams);
        }
    }
}