using System.Security.Cryptography;
using System.Text;

namespace MyVirtualAcademy.Helper
{
    public class HelperCryptography
    {
        public static string GenerateSalt()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(50));
        }

        public static bool CompararArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public static byte[] EncryptPassword(string password, string salt)
        {
            string contenido = password + salt;
            using SHA256 managed = SHA256.Create();
            byte[] salida = Encoding.UTF8.GetBytes(contenido);
            for (int i = 1; i <= 20; i++)
                salida = managed.ComputeHash(salida);
            return salida;
        }

        public static string HashPasswordBCrypt(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public static bool VerifyPasswordBCrypt(string password, string hash)
            => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
