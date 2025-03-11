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
                                expires: DateTime.UtcNow.AddMinutes(60),
                                signingCredentials: new SigningCredentials(jwtKey, SecurityAlgorithms.HmacSha256)
                        );

                        return new JwtSecurityTokenHandler().WriteToken(jwt);
                }

                public string CreateRefreshToken()
                {
                        return Guid.NewGuid().ToString();
                }

                public ClaimsPrincipal GetJwtToken(string token)
                {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var key = Encoding.UTF8.GetBytes(_configuration["JWT:KEY"]);

                        var parameters = new TokenValidationParameters
                        {
                                ValidateIssuer = true,
                                ValidIssuer = _configuration["Jwt:Issuer"],
                                ValidateAudience = true,
                                ValidAudience = _configuration["Jwt:Audience"],
                                ValidateLifetime = true,
                                IssuerSigningKey = new SymmetricSecurityKey(key),
                                ClockSkew = TimeSpan.FromMinutes(5)
                        };

                        return tokenHandler.ValidateToken(token, parameters, out _);
                }

                public async Task<bool> IsRefreshFkAccessToken(string accessToken, string refreshToken)
                {
                        var refreshTokenResult = await _context.UserRefreshTokens
                            .Where(y => y.RefreshToken == refreshToken)
                            .Select(y => y.UserId)
                            .FirstOrDefaultAsync();

                        if (refreshTokenResult == default) return false;

                        var accessTokenResult = await _context.Token
                            .Where(t => t.AccessToken == accessToken && t.UserId == refreshTokenResult)
                            .FirstOrDefaultAsync();

                        return accessTokenResult != null;
                }

                public async Task<bool> IsRefreshTokenRevoked()
                {
                        var refreshToken = _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];
                        var result = await _context.UserRefreshTokens.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
                        if (result == null) return true;
                        return result.ExpiryDateTime.ToUniversalTime() < DateTime.UtcNow;
                }

                public async Task<bool> IsAccessTokenRevoked()
                {
                        var authToken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                        var result = await _context.Token.FirstOrDefaultAsync(x => x.AccessToken == authToken);
                        if (result == null) return true;
                        return (result.ExpiryDateTime.ToUniversalTime() < DateTime.UtcNow) || result.IsRevoked == true;
                }
        }
}
