using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Website_Mua_Ban_Rao_Vat.Models
{
    public static class IdProtector
    {
        private static string secretKey = "duyot01235894065nguyen";

        public static string EncryptId(int id)
        {
            var plainText = $"{id}-{secretKey}";
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainBytes)
                         .Replace("=", "")
                         .Replace('+', '-')
                         .Replace('/', '_');
        }
        public static int? DecryptId(string encoded)
        {
            try
            {
                encoded = encoded.Replace('-', '+').Replace('_', '/');
                switch (encoded.Length % 4)
                {
                    case 2: encoded += "=="; break;
                    case 3: encoded += "="; break;
                }

                var bytes = Convert.FromBase64String(encoded);
                var decoded = Encoding.UTF8.GetString(bytes);
                var parts = decoded.Split('-');
                if (parts.Length == 2 && parts[1] == secretKey)
                {
                    return int.Parse(parts[0]);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
            return null;
        }
    }
}