using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class CloudinaryService
    {
        private Cloudinary cloudinary;
        public CloudinaryService()
        {
            Account account = new Account(
                ConfigurationManager.AppSettings["Cloudinary.CloudName"],
                ConfigurationManager.AppSettings["Cloudinary.ApiKey"],
                ConfigurationManager.AppSettings["Cloudinary.ApiSecret"]
            );
            cloudinary = new Cloudinary(account);
        }
        public ImageUploadResult Upload(ImageUploadParams uploadParams)
        {
            return cloudinary.Upload(uploadParams);
        }
        public DeletionResult Delete(DeletionParams deletionParams)
        {
            return cloudinary.Destroy(deletionParams);
        }
    }
}