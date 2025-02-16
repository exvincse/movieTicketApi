using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using movieTickApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace movieTickApi.Service
{
        public class TokenService
        {
                private readonly IConfiguration _configuration;
                private readonly WebDbContext _context;
                private readonly IHttpContextAccessor _httpContextAccessor;

                public TokenService(IConfiguration configuration, WebDbContext context, IHttpContextAccessor httpContextAccessor)
                {
                        _configuration = configuration;
                        _context = context;
                        _httpContextAccessor = httpContextAccessor;
                }

                public string CreateAccessToken(IEnumerable<Claim> claim)
                {

                        var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));

                        var jwt = new JwtSecurityToken(
                                issuer: _configuration["JWT:Issuer"],
                                audience: _configuration["JWT:Audience"],
                                claims: claim,
                                expires: DateTime.UtcNow.AddMinutes(5),
                                signingCredentials: new SigningCredentials(jwtKey, SecurityAlgorithms.HmacSha256)
                        );

                        return new JwtSecurityTokenHandler().WriteToken(jwt);
                }

                public string CreateRefreshToken()
                {
                        return Guid.NewGuid().ToString();
                }

                public ClaimsPrincipal GetPrincipalFromToken()
                {
                        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        var tokenHandler = new JwtSecurityTokenHandler();
                        SecurityToken validatedToken;

                        var jwtUser = tokenHandler.ValidateToken(token, new TokenValidationParameters
                        {
                                ValidateIssuer = true,
                                ValidIssuer = _configuration["Jwt:Issuer"],
                                ValidateAudience = true,
                                ValidAudience = _configuration["Jwt:Audience"],
                                ValidateLifetime = true,
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:KEY"])),
                                ClockSkew = TimeSpan.Zero
                        }, out validatedToken);

                        return jwtUser;
                }

                public async Task<bool>  IsTokenRevokedAsync()
                {
                        var refreshToken = _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];
                        var result = await _context.UserRefreshTokens.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
                        if (result == null) return false;
                        return result.ExpiryDate < DateTime.UtcNow;
                }

                public async Task<bool?> IsAccessTokenRevoked()
                {
                        var authToken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        var result = await _context.Token.FirstOrDefaultAsync(x => x.token == authToken);
                        if (result == null) return null;
                        return result.ExpiresAt < DateTime.UtcNow;
                }
        }
}
