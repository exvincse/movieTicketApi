
using Microsoft.IdentityModel.Tokens;
using movieTickApi.Service;

namespace movieTickApi.Middleware
{
        public class TokenValidation
        {
                private readonly RequestDelegate _next;
                public TokenValidation(RequestDelegate next)
                {
                        _next = next;
                }

                public async Task InvokeAsync(HttpContext context, TokenService tokenService)
                {
                        try
                        {
                                var request = context.Request;

                                // 檢查是否是刷新 token 的請求
                                if (request.Path.StartsWithSegments("/api/User/RefreshToken"))
                                {
                                        return;
                                }

                                if (await tokenService.IsAccessTokenRevoked())
                                {
                                        var claimsPrincipal = tokenService.GetPrincipalFromToken();

                                        if (claimsPrincipal != null)
                                        {
                                                context.User = claimsPrincipal;
                                        }
                                }
                        }
                        catch (SecurityTokenExpiredException)
                        {
                                await WriteErrorResponse(context, "Token 已過期", 401, false, true);
                                return;
                        }
                        catch (Exception ex)
                        {
                                Console.WriteLine($"JWT 驗證錯誤: {ex.Message}");
                        }

                        await _next(context);
                }

                async Task WriteErrorResponse(HttpContext context, string message, int statusCode, bool isRepeatLogin, bool isReNewToken)
                {
                        context.Response.StatusCode = statusCode;
                        context.Response.ContentType = "application/json";
                        var errorResponse = new
                        {
                                StatusCode = statusCode,
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
}
