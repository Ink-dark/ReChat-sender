using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HalloChat_CSharp.Services
{
    public class ApiService
    {
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        private ClientWebSocket webSocket;
        private string baseUrl = "http://localhost:7932";
        private string webSocketUrl = "ws://localhost:7932/ws";
        private bool isConnected = false;

        public event Action<string> MessageReceived;
        public event Action<bool> ConnectionStatusChanged;

        public async Task InitializeAsync(string serverAddress = "localhost", string port = "7932")
        {
            baseUrl = $"http://{serverAddress}:{port}";
            webSocketUrl = $"ws://{serverAddress}:{port}/ws";
            
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<HttpResponseMessage> SendRequestAsync<T>(string endpoint, HttpMethod method, T data = null)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
                
                if (data != null)
                {
                    string json = JsonConvert.SerializeObject(data);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                return await httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API 请求失败: {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                HttpResponseMessage response = await SendRequestAsync(endpoint, HttpMethod.Get);
                response.EnsureSuccessStatusCode();
                
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GET 请求失败: {ex.Message}");
                throw;
            }
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                HttpResponseMessage response = await SendRequestAsync(endpoint, HttpMethod.Post, data);
                response.EnsureSuccessStatusCode();
                
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST 请求失败: {ex.Message}");
                throw;
            }
        }

        public async Task ConnectWebSocketAsync(int retryCount = 3)
        {
            int attempt = 0;
            while (attempt < retryCount)
            {
                try
                {
                    webSocket = new ClientWebSocket();
                    await webSocket.ConnectAsync(new Uri(webSocketUrl), System.Threading.CancellationToken.None);
                    isConnected = true;
                    ConnectionStatusChanged?.Invoke(true);
                    
                    _ = ReceiveWebSocketMessagesAsync();
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"WebSocket 连接失败 (尝试 {attempt}/{retryCount}): {ex.Message}");
                    if (attempt >= retryCount)
                    {
                        isConnected = false;
                        ConnectionStatusChanged?.Invoke(false);
                        throw;
                    }
                    // 等待后重试
                    await Task.Delay(1000 * attempt);
                }
            }
        }

        private async Task ReceiveWebSocketMessagesAsync()
        {
            byte[] buffer = new byte[1024 * 4];
            
            while (true)
            {
                try
                {
                    if (webSocket == null || webSocket.State != WebSocketState.Open)
                    {
                        // 尝试重连
                        await Task.Delay(3000);
                        try
                        {
                            await ConnectWebSocketAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"重连失败: {ex.Message}");
                            continue;
                        }
                    }
                    
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        MessageReceived?.Invoke(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, System.Threading.CancellationToken.None);
                        isConnected = false;
                        ConnectionStatusChanged?.Invoke(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket 接收消息失败: {ex.Message}");
                    isConnected = false;
                    ConnectionStatusChanged?.Invoke(false);
                    // 等待后重试
                    await Task.Delay(3000);
                }
            }
        }

        public async Task SendWebSocketMessageAsync(string message)
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            }
        }

        public async Task DisconnectWebSocketAsync()
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, System.Threading.CancellationToken.None);
                isConnected = false;
                ConnectionStatusChanged?.Invoke(false);
            }
        }

        public bool IsConnected => isConnected;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class MessageRequest
    {
        public string Platform { get; set; }
        public string Recipient { get; set; }
        public string Content { get; set; }
    }

    public class PlatformAccount
    {
        public string Id { get; set; }
        public string Platform { get; set; }
        public string AccountName { get; set; }
        public string Status { get; set; }
    }

    public class ChatMessage
    {
        public string Id { get; set; }
        public string Platform { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSent { get; set; }
        public bool IsRead { get; set; }
    }
}
