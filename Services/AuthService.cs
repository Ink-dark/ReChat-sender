using System;
using System.Threading.Tasks;
using HalloChat_CSharp.Services;

namespace HalloChat_CSharp.Services
{
    public class AuthService
    {
        private ApiService apiService;

        public event Action<bool> AuthStatusChanged;

        public AuthService(ApiService apiService)
        {
            this.apiService = apiService;
        }

        public async Task<UserInfo> LoginAsync(string username, string password, bool isAdmin = false)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password,
                    IsAdmin = isAdmin
                };

                var response = await apiService.PostAsync<ApiResponse<UserInfo>>("/api/auth/login", loginRequest);
                
                if (response.Success)
                {
                    AuthStatusChanged?.Invoke(true);
                    return response.Data;
                }
                else
                {
                    AuthStatusChanged?.Invoke(false);
                    throw new Exception(response.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"登录失败: {ex.Message}");
                AuthStatusChanged?.Invoke(false);
                throw;
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var response = await apiService.PostAsync<ApiResponse<bool>>("/api/auth/logout", null);
                
                if (response.Success)
                {
                    AuthStatusChanged?.Invoke(false);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"登出失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var response = await apiService.PostAsync<ApiResponse<bool>>("/api/auth/refresh", null);
                return response.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷新令牌失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckAuthStatusAsync()
        {
            try
            {
                var response = await apiService.GetAsync<ApiResponse<bool>>("/api/auth/status");
                return response.Success && response.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查认证状态失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnableAutoLoginAsync(bool enable)
        {
            try
            {
                var response = await apiService.PostAsync<ApiResponse<bool>>("/api/auth/auto-login", new { enable });
                return response.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置自动登录失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await apiService.GetAsync<ApiResponse<bool>>("/api/auth/test");
                return response.Success && response.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试连接失败: {ex.Message}");
                return false;
            }
        }
    }
}
