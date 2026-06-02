using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL
{
    public class clsPasswordHasher
    {// 1. توليد ملح عشوائي (Salt)
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        // 2. دالة خلط الباسورد مع السالت وإنتاج الـ Hash
        public static string ComputeHash(string password, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // ندمج الباسورد مع السالت
                string combinedPassword = password + salt;
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedPassword));

                // تحويل المصفوفة إلى نص للقاعدة
                return Convert.ToBase64String(bytes);
            }
        }


        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            // نحسب الهاش للباسورد المدخل باستخدام السالت المخزنة
            string hashOfEnteredPassword = ComputeHash(enteredPassword, storedSalt);
            // نقارن الهاش المحسوب مع الهاش المخزن
            return hashOfEnteredPassword == storedHash;
        }
    }
}
