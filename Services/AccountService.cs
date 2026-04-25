using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HalloChat_CSharp.Services;

namespace HalloChat_CSharp.Services
{
    public class AccountService
    {
        private ApiService apiService;

        public AccountService(ApiService apiService)
        {
            this.apiService = apiService;
        }

        public async Task<List<PlatformAccount>> GetPlatformAccountsAsync()
        {
            try
            {
                return await apiService.GetAsync<List<PlatformAccount>>("/api/accounts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取平台账号失败: {ex.Message}");
                return new List<PlatformAccount>();
            }
        }

        public async Task<PlatformAccount> AddPlatformAccountAsync(PlatformAccount account)
        {
            try
            {
                var response = await apiService.PostAsync<ApiResponse<PlatformAccount>> ("/api/accounts", account);
                return response.Success ? response.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加平台账号失败: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RemovePlatformAccountAsync(string accountId)
        {
            try
            {
                var response = await apiService.SendRequestAsync($"/api/accounts/{accountId}", System.Net.Http.HttpMethod.Delete);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除平台账号失败: {ex.Message}");
                return false;
            }
        }

        public async Task<PlatformAccount> UpdatePlatformAccountAsync(PlatformAccount account)
        {
            try
            {
                var response = await apiService.PostAsync<ApiResponse<PlatformAccount>>($"/api/accounts/{account.Id}", account);
                return response.Success ? response.Data : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新平台账号失败: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> TestPlatformConnectionAsync(string accountId)
        {
            try
            {
                var response = await apiService.GetAsync<ApiResponse<bool>>($"/api/accounts/{accountId}/test");
                return response.Success && response.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试平台连接失败: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetSupportedPlatformsAsync()
        {
            try
            {
                var response = await apiService.GetAsync<ApiResponse<List<string>>>("/api/platforms");
                return response.Success ? response.Data : new List<string> { "QQ", "微信" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取支持的平台失败: {ex.Message}");
                return new List<string> { "QQ", "微信" };
            }
        }
    }
}
