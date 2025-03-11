using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using movieTickApi.Service;
using System.Security.Claims;

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
                if (authorizeAttribute == null)
                {
                        await _next(context);
                        return;
                }

                var refreshToken = context.Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken) == true)
                {
                        await UnauthorizedResponseHandle(context, "請重新登入", true, false);
                        return;
                }

                var acccessToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");


                if (string.IsNullOrEmpty(acccessToken) == true)
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
                        var jwtToken = tokenService.GetJwtToken(acccessToken);

                        if (jwtToken == null)
                        {
                                await UnauthorizedResponseHandle(context, "無效的token", true, false);
                                return;
                        };

                        var jwtTokenValid = await tokenService.IsRefreshFkAccessToken(acccessToken, refreshToken);
                        if (jwtTokenValid == false)
                        {
                                await UnauthorizedResponseHandle(context, "無效的token", true, false);
                                return;
                        }

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

