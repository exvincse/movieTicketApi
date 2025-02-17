using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using movieTickApi.Service;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public class TokenValidationMiddleware
{
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public TokenValidationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
                _next = next;
                _configuration = configuration;
        }

        public async Task Invoke(HttpContext context, TokenService tokenService)
        {

                var endpoint = context.GetEndpoint();
                var authorizeAttribute = endpoint?.Metadata?.GetMetadata<AuthorizeAttribute>();
                if (authorizeAttribute == null) {
                        await _next(context);
                        return;
                }

                if (string.IsNullOrEmpty(context.Request.Cookies["refreshToken"]) == true)
                {
                        await UnauthorizedResponseHandle(context, "請重新登入", true, false);
                        return;
                }

                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");


                if (string.IsNullOrEmpty(token) == true)
                {
                        // 判斷http  cookie設置refresh token是否過期
                        var IsRefreshTokenRevoked = await tokenService.IsRefreshTokenRevoked();
                        if (IsRefreshTokenRevoked == true)
                        {
                                await UnauthorizedResponseHandle(context, "請重新登入", true, false);
                                return;
                        }
                        else
                        {
                                await UnauthorizedResponseHandle(context, "token已過期", false, true);
                                return;
                        }
                } 
                else
                {
                        var IsAccessTokenRevoked = await tokenService.IsAccessTokenRevoked();
                        var IsRefreshTokenRevoked = await tokenService.IsRefreshTokenRevoked();

                        if (IsRefreshTokenRevoked == true)
                        {
                                await UnauthorizedResponseHandle(context, "請重新登入", true, false);
                                return;
                        }

                        if (IsAccessTokenRevoked == true)
                        {
                                await UnauthorizedResponseHandle(context, "token已過期", false, true);
                                return;
                        }
                }

                try
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
                                IssuerSigningKey = new SymmetricSecurityKey(key)
                        };

                        var jwtToken = tokenHandler.ValidateToken(token, parameters, out _); 

                        if (jwtToken == null)
                        {
                                await UnauthorizedResponseHandle(context, "無效的token", true, false);
                                return;
                        };

                        context.User = jwtToken;

                        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                        context.Items["UserId"] = userId?.Value;
                }
                catch (SecurityTokenExpiredException)
                {
                        await UnauthorizedResponseHandle(context, "無效的token", true, false);
                        return;
                }

                await _next(context);
        }

        private static async Task UnauthorizedResponseHandle(HttpContext context, string message, bool isRepeatLogin, bool isReNewToken)
        {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                        StatusCode = 401,
                        Message = message,
                        Result = new
                        {
                                isRepeatLogin,
                                isReNewToken
                        }
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
        }
}

