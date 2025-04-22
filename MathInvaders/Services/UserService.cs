using MathInvaders.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MathInvaders.Services
{
    public class UserService
    {
        private readonly List<User> _users = new List<User>(); // Для простоты храним в памяти

        public User Register(string name, string email, string password)
        {
            // Проверяем, существует ли пользователь с таким email
            if (_users.Any(u => u.Email == email))
            {
                throw new Exception("Пользователь с таким email уже существует!");
            }

            // Генерируем соль
            var salt = GenerateSalt();
            // Хешируем пароль с солью
            var passwordHash = HashPassword(password, salt);

            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = passwordHash,
                Salt = salt
            };

            _users.Add(user);
            return user;
        }

        public User Login(string email, string password)
        {
            var user = _users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                throw new Exception("Пользователь не найден!");
            }

            // Хешируем введённый пароль с солью пользователя
            var passwordHash = HashPassword(password, user.Salt);
            if (passwordHash != user.PasswordHash)
            {
                throw new Exception("Неверный пароль!");
            }

            return user;
        }

        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            // Используем PBKDF2 с HMAC-SHA256
            byte[] saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32); // 256 бит
                return Convert.ToBase64String(hash);
            }
        }
    }
}