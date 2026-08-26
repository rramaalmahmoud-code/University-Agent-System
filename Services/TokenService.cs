using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using University_Agent_System.Models;
using University_Agent_System.Models.ViewModel;
using Dapper;


namespace University_Agent_System.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;
        private readonly IDbConnection _db;

        public TokenService(IConfiguration config, IDbConnection db)
        {
            _config = config;
            _db = db;
        }

        public string GenerateToken(user user)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.userId.ToString()),
        new Claim(ClaimTypes.Name, user.userName),
    };

            if (user.UserType != null)
            {
                // Keep Role (optional for ASP.NET role-based auth)
                claims.Add(new Claim(ClaimTypes.Role, user.UserType.userTypeEnglish));

                // Add explicit userType for Razor and manual use
                claims.Add(new Claim("userType", user.UserType.userTypeEnglish));  // 👈 This is what your Razor code is looking for
                claims.Add(new Claim("userId", user.userId.ToString()));  // 👈 This is what your Razor code is looking for
            }

            var agent = _db.QueryFirstOrDefault<agent>("SELECT agentId FROM Agents WHERE userId = @userId", new { user.userId });
            if (agent != null)
            {
                claims.Add(new Claim("agentId", agent.agentId.ToString()));
            }



            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
