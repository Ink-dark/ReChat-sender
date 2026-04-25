using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HalloChat_CSharp.Services;

namespace HalloChat_CSharp.Services
{
    public class SyncService
    {
        private ApiService apiService;
        private MessageService messageService;
        private AccountService accountService;

        public event Action<ChatMessage> CrossPlatformMessageReceived;
        public event Action<ChatMessage> MessageSynced;
        public event Action<bool> SyncStatusChanged;

        private bool isSyncing = false;
        private Dictionary<string, DateTime> lastSyncTimeByPlatform;
        private string selectedPlatform = "QQ";

        public SyncService(ApiService apiService, MessageService messageService, AccountService accountService)
        {
            this.apiService = apiService;
            this.messageService = messageService;
            this.accountService = accountService;
            this.lastSyncTimeByPlatform = new Dictionary<string, DateTime>();
        }

        public async Task StartSyncAsync()
        {
            try
            {
                isSyncing = true;
                SyncStatusChanged?.Invoke(true);
                
                // 启动消息同步循环
                _ = SyncLoopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动同步失败: {ex.Message}");
                isSyncing = false;
                SyncStatusChanged?.Invoke(false);
            }
        }

        public async Task StopSyncAsync()
        {
            isSyncing = false;
            SyncStatusChanged?.Invoke(false);
        }

        private async Task SyncLoopAsync()
        {
            while (isSyncing)
            {
                try
                {
                    await SyncAllPlatformsAsync();
                    await Task.Delay(5000); // 每5秒同步一次
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"同步循环错误: {ex.Message}");
                    await Task.Delay(10000); // 出错后延迟10秒重试
                }
            }
        }

        private async Task SyncAllPlatformsAsync()
        {
            try
            {
                var platforms = await accountService.GetSupportedPlatformsAsync();
                
                foreach (var platform in platforms)
                {
                    await SyncPlatformAsync(platform);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步所有平台失败: {ex.Message}");
            }
        }

        private async Task SyncPlatformAsync(string platform)
        {
            try
            {
                var lastSyncTime = lastSyncTimeByPlatform.ContainsKey(platform) 
                    ? lastSyncTimeByPlatform[platform] 
                    : DateTime.MinValue;

                var endpoint = $"/api/sync/messages?platform={platform}&since={lastSyncTime.ToUniversalTime():o}";
                var response = await apiService.GetAsync<ApiResponse<List<ChatMessage>>>(endpoint);
                
                if (response.Success && response.Data != null)
                {
                    foreach (var message in response.Data)
                    {
                        // 处理同步过来的消息
                        await ProcessSyncedMessageAsync(message);
                    }

                    // 更新最后同步时间
                    lastSyncTimeByPlatform[platform] = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步平台 {platform} 失败: {ex.Message}");
            }
        }

        private async Task ProcessSyncedMessageAsync(ChatMessage message)
        {
            try
            {
                // 触发消息同步事件
                MessageSynced?.Invoke(message);
                
                // 如果是跨平台消息，触发跨平台消息事件
                if (message.Platform != selectedPlatform)
                {
                    CrossPlatformMessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理同步消息失败: {ex.Message}");
            }
        }

        public async Task ForwardMessageAsync(string sourcePlatform, string targetPlatform, string messageId)
        {
            try
            {
                var forwardRequest = new
                {
                    SourcePlatform = sourcePlatform,
                    TargetPlatform = targetPlatform,
                    MessageId = messageId
                };

                var response = await apiService.PostAsync<ApiResponse<bool>>("/api/sync/forward", forwardRequest);
                
                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转发消息失败: {ex.Message}");
                throw;
            }
        }

        public async Task SyncContactsAsync()
        {
            try
            {
                var response = await apiService.GetAsync<ApiResponse<bool>>("/api/sync/contacts");
                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"同步联系人失败: {ex.Message}");
                throw;
            }
        }

        public async Task GetSyncStatusAsync()
        {
            try
            {
                var response = await apiService.GetAsync<ApiResponse<Dictionary<string, string>>>("/api/sync/status");
                if (response.Success && response.Data != null)
                {
                    foreach (var kvp in response.Data)
                    {
                        Console.WriteLine($"平台 {kvp.Key}: {kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取同步状态失败: {ex.Message}");
            }
        }

        public bool IsSyncing => isSyncing;

        public void SetSelectedPlatform(string platform)
        {
            selectedPlatform = platform;
        }
    }
}
