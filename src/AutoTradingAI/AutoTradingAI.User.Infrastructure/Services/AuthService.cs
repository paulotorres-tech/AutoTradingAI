using AutoTradingAI.User.Core.Interfaces;
using AutoTradingAI.User.Core.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens;

namespace AutoTradingAI.User.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IMongoCollection<UserRecord> _users;
        private readonly IConfiguration _configuration;

        public AuthService(IMongoClient client, IConfiguration configuration)
        {
            _configuration = configuration;
            var database = client.GetDatabase(configuration["Database:Name"]);
            _users = database.GetCollection<UserRecord>(configuration["Database:Collections:Users"]);
        }

        public async Task<UserRecord> RegisterAsync(string username, string email, string password)
        {
            // check if user exists
            var existingUser = await _users.Find(u => u.Email == email).FirstAsync();
            if (existingUser != null)
            {
                throw new Exception("User already exists");
            }


            // create new user record
            var newUser = new UserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            // save new user on database
            await _users.InsertOneAsync(newUser);

            return newUser;
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            // get user record from database by email
            var user = await _users.Find(u => u.Email == email).FirstAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password");
            }

            // generate JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, string.Join(",", user.Roles))
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }


    }
}
