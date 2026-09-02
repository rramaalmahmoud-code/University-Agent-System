using Dapper;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using University_Agent_System.Models;

namespace University_Agent_System.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;
        private readonly IDbConnection _db;

        public TokenService(
            IConfiguration config,
            IDbConnection db)
        {
            _config = config;
            _db = db;
        }

        public string GenerateToken(user user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var agentProfile =
                _db.QueryFirstOrDefault<AgentTokenProfile>(@"
SELECT TOP (1)
    agentId AS AgentId,
    agentNameEnglish AS AgentNameEnglish,
    agentNameArabic AS AgentNameArabic
FROM Agents
WHERE userId = @UserId",
                new
                {
                    UserId = user.userId
                });

            /*
             * For an Agent:
             * Use the agent's actual Arabic and English names.
             *
             * For Admin/Super Admin:
             * Use Users.userName because there is no agent record.
             */
            var displayNameEnglish =
                agentProfile?.AgentNameEnglish?.Trim();

            if (string.IsNullOrWhiteSpace(displayNameEnglish))
            {
                displayNameEnglish =
                    user.userName?.Trim() ?? "User";
            }

            var displayNameArabic =
                agentProfile?.AgentNameArabic?.Trim();

            if (string.IsNullOrWhiteSpace(displayNameArabic))
            {
                displayNameArabic = displayNameEnglish;
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.userId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    displayNameEnglish),

                new Claim(
                    "userId",
                    user.userId.ToString()),

                new Claim(
                    "userName",
                    user.userName ?? string.Empty),

                new Claim(
                    "displayNameEnglish",
                    displayNameEnglish),

                new Claim(
                    "displayNameArabic",
                    displayNameArabic)
            };

            if (user.UserType != null &&
                !string.IsNullOrWhiteSpace(
                    user.UserType.userTypeEnglish))
            {
                var userType =
                    user.UserType.userTypeEnglish.Trim();

                claims.Add(new Claim(
                    ClaimTypes.Role,
                    userType));

                claims.Add(new Claim(
                    "userType",
                    userType));
            }

            if (agentProfile != null)
            {
                claims.Add(new Claim(
                    "agentId",
                    agentProfile.AgentId.ToString()));
            }

            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "Jwt:Key is missing from configuration.");
            }

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        private sealed class AgentTokenProfile
        {
            public int AgentId { get; set; }

            public string AgentNameEnglish { get; set; }

            public string AgentNameArabic { get; set; }
        }
    }
}