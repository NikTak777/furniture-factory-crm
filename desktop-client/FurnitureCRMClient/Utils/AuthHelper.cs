using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace FurnitureCRMClient.Utils
{
    public static class AuthHelper
    {
        public static string GenerateRandomPassword(int length = 12)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateRandomUsername(string baseName, int length = 6)
        {
            var random = new Random();

            // Извлекаем первое слово из ФИО
            string firstWord = baseName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "user";

            // Генерируем 6 случайных цифр
            string suffix = new string(Enumerable.Repeat("0123456789", length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return firstWord + suffix;
        }
    }
}
