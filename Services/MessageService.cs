using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HalloChat_CSharp.Services;

namespace HalloChat_CSharp.Services
{
    public class MessageService
    {
        private ApiService apiService;

        public event Action<ChatMessage> MessageReceived;
        public event Action<ChatMessage> MessageSent;
        public event Action<ChatMessage> MessageStatusChanged;

        public MessageService(ApiService apiService)
        {
            this.apiService = apiService;
            this.apiService.MessageReceived += OnApiMessageReceived;
        }

        private void OnApiMessageReceived(string message)
        {
            try
            {
                var chatMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<ChatMessage>(message);
                MessageReceived?.Invoke(chatMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理消息失败: {ex.Message}");
            }
        }

        public async Task<ChatMessage> SendMessageAsync(string platform, string recipient, string content)
        {
            try
            {
                var messageRequest = new MessageRequest
                {
                    Platform = platform,
                    Recipient = recipient,
                    Content = content
                };

                var response = await apiService.PostAsync<ApiResponse<ChatMessage>>("/api/messages/send", messageRequest);
                
                if (response.Success)
                {
                    var chatMessage = response.Data;
                    MessageSent?.Invoke(chatMessage);
                    return chatMessage;
                }
                else
                {
                    throw new Exception(response.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ChatMessage>> GetMessageHistoryAsync(string platform, string contact, int limit = 50, int offset = 0)
        {
            try
            {
                var endpoint = $"/api/messages/history?platform={platform}&contact={contact}&limit={limit}&offset={offset}";
                var response = await apiService.GetAsync<ApiResponse<List<ChatMessage>>>(endpoint);
                return response.Success ? response.Data : new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取消息历史失败: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public async Task MarkMessageAsReadAsync(string messageId)
        {
            try
            {
                var response = await apiService.PostAsync<ApiResponse<bool>>($"/api/messages/{messageId}/read", null);
                if (response.Success)
                {
                    // 触发消息状态变更事件
                    MessageStatusChanged?.Invoke(new ChatMessage { Id = messageId, IsRead = true });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"标记消息已读失败: {ex.Message}");
            }
        }

        public async Task<List<ChatMessage>> GetUnreadMessagesAsync()
        {
            try
            {
                var response = await apiService.GetAsync<ApiResponse<List<ChatMessage>>>("/api/messages/unread");
                return response.Success ? response.Data : new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取未读消息失败: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId)
        {
            try
            {
                var response = await apiService.SendRequestAsync($"/api/messages/{messageId}", System.Net.Http.HttpMethod.Delete);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除消息失败: {ex.Message}");
                return false;
            }
        }

        public string FormatMessageContent(string content, string platform)
        {
            // 根据不同平台格式化消息内容
            switch (platform.ToLower())
            {
                case "qq":
                    // QQ 消息格式处理
                    return content;
                case "微信":
                case "wechat":
                    // 微信消息格式处理
                    return content;
                default:
                    return content;
            }
        }

        public ChatMessage ConvertToChatMessage(dynamic platformMessage, string platform)
        {
            // 将不同平台的消息格式转换为统一的 ChatMessage 格式
            try
            {
                return new ChatMessage
                {
                    Id = platformMessage.id?.ToString() ?? Guid.NewGuid().ToString(),
                    Platform = platform,
                    Sender = platformMessage.sender?.ToString() ?? "",
                    Recipient = platformMessage.recipient?.ToString() ?? "",
                    Content = platformMessage.content?.ToString() ?? "",
                    Timestamp = platformMessage.timestamp != null ? DateTime.Parse(platformMessage.timestamp.ToString()) : DateTime.Now,
                    IsSent = platformMessage.isSent ?? false,
                    IsRead = platformMessage.isRead ?? false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换消息格式失败: {ex.Message}");
                return null;
            }
        }
    }
}
