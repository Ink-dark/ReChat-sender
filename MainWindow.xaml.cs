﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;

namespace HalloChat_CSharp
{
    using Services;

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private UserInfo currentUser;
        private ApiService apiService;
        private AccountService accountService;
        private MessageService messageService;
        private AuthService authService;
        private SyncService syncService;
        private List<PlatformAccount> platformAccounts;
        private List<ChatMessage> messages;
        private string selectedContact;
        private string selectedPlatform;

        public MainWindow(UserInfo userInfo)
        {
            InitializeComponent();
            currentUser = userInfo;
            InitializeServices();
            LoadPlatformAccounts();
            LoadMessages();
            StartSync();
            UpdateUI();
        }

        private void InitializeServices()
        {
            apiService = new ApiService();
            apiService.ConnectionStatusChanged += OnConnectionStatusChanged;
            authService = new AuthService(apiService);
            authService.AuthStatusChanged += OnAuthStatusChanged;
            accountService = new AccountService(apiService);
            messageService = new MessageService(apiService);
            messageService.MessageReceived += OnMessageReceived;
            messageService.MessageSent += OnMessageSent;
            messageService.MessageStatusChanged += OnMessageStatusChanged;
            syncService = new SyncService(apiService, messageService, accountService);
            syncService.CrossPlatformMessageReceived += OnCrossPlatformMessageReceived;
            syncService.MessageSynced += OnMessageSynced;
            syncService.SyncStatusChanged += OnSyncStatusChanged;
        }

        private async void StartSync()
        {
            await syncService.StartSyncAsync();
        }

        private void OnAuthStatusChanged(bool isAuthenticated)
        {
            if (!isAuthenticated)
            {
                // 如果认证状态变为未认证，跳转到登录窗口
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private async void LoadPlatformAccounts()
        {
            try
            {
                // 从后端获取平台账号列表
                platformAccounts = await accountService.GetPlatformAccountsAsync();
                // 更新界面显示
                UpdatePlatformAccounts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载平台账号失败: {ex.Message}");
                // 使用模拟数据
                platformAccounts = new List<PlatformAccount>
                {
                    new PlatformAccount { Id = "1", Platform = "QQ", AccountName = "QQ账号1", Status = "在线" },
                    new PlatformAccount { Id = "2", Platform = "微信", AccountName = "微信账号1", Status = "在线" }
                };
                UpdatePlatformAccounts();
            }
        }

        private void UpdatePlatformAccounts()
        {
            // 这里可以更新平台账号列表的显示
            Console.WriteLine($"已加载 {platformAccounts.Count} 个平台账号");
        }

        private async void LoadMessages()
        {
            messages = new List<ChatMessage>();
            // 可以从后端加载历史消息
        }

        private void UpdateUI()
        {
            // 更新用户信息
            UsernameText.Text = currentUser.Username;
            EmailText.Text = currentUser.Email;
            UserIdText.Text = "ID: " + currentUser.Id;

            // 更新系统状态
            ConnectionStatusText.Text = apiService.IsConnected ? "已连接" : "未连接";
            LastOnlineText.Text = "最后在线: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // 更新聊天窗口标题
            ChatTitleText.Text = "选择联系人开始聊天";
            ChatStatusText.Text = "";
        }

        private void OnMessageReceived(ChatMessage message)
        {
            try
            {
                messages.Add(message);
                // 更新消息列表显示
                UpdateMessageList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理消息失败: {ex.Message}");
            }
        }

        private void OnMessageSent(ChatMessage message)
        {
            try
            {
                messages.Add(message);
                // 更新消息列表显示
                UpdateMessageList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理发送消息失败: {ex.Message}");
            }
        }

        private void OnMessageStatusChanged(ChatMessage message)
        {
            try
            {
                var existingMessage = messages.FirstOrDefault(m => m.Id == message.Id);
                if (existingMessage != null)
                {
                    existingMessage.IsRead = message.IsRead;
                    // 更新消息列表显示
                    UpdateMessageList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理消息状态变更失败: {ex.Message}");
            }
        }

        private void OnCrossPlatformMessageReceived(ChatMessage message)
        {
            try
            {
                // 处理跨平台消息
                Console.WriteLine($"收到跨平台消息: {message.Platform} -> {message.Content}");
                messages.Add(message);
                UpdateMessageList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理跨平台消息失败: {ex.Message}");
            }
        }

        private void OnMessageSynced(ChatMessage message)
        {
            try
            {
                // 处理同步消息
                Console.WriteLine($"同步消息: {message.Platform} -> {message.Content}");
                var existingMessage = messages.FirstOrDefault(m => m.Id == message.Id);
                if (existingMessage == null)
                {
                    messages.Add(message);
                    UpdateMessageList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理同步消息失败: {ex.Message}");
            }
        }

        private void OnSyncStatusChanged(bool isSyncing)
        {
            try
            {
                // 更新同步状态显示
                Console.WriteLine($"同步状态: {(isSyncing ? "正在同步" : "已停止同步")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理同步状态变更失败: {ex.Message}");
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            ConnectionStatusText.Text = isConnected ? "已连接" : "未连接";
        }

        private void UpdateMessageList()
        {
            // 这里可以更新消息列表的显示
            Console.WriteLine($"消息列表已更新，共 {messages.Count} 条消息");
        }

        private void ServerItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 处理服务器选择
            MessageBox.Show("服务器选择功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ContactItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 处理联系人选择
            var contact = (ContactInfo)((FrameworkElement)sender).DataContext;
            selectedContact = contact.Username;
            ChatTitleText.Text = contact.Username;
            ChatStatusText.Text = contact.Status;
        }

        private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedContact) || string.IsNullOrEmpty(MessageInput.Text))
            {
                MessageBox.Show("请选择联系人和输入消息", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                await messageService.SendMessageAsync(selectedPlatform ?? "QQ", selectedContact, MessageInput.Text);
                
                // 清空输入框
                MessageInput.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送消息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddContactButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理添加联系人
            MessageBox.Show("添加联系人功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateGroupButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理创建群聊
            MessageBox.Show("创建群聊功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EncryptChatButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理加密聊天
            MessageBox.Show("加密聊天功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MoreOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理更多选项
            MessageBox.Show("更多选项功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理打开设置
            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理关闭设置
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理保存设置
            MessageBox.Show("设置保存功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理登出
            try
            {
                // 停止同步
                await syncService.StopSyncAsync();
                await authService.LogoutAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"登出失败: {ex.Message}");
            }
            finally
            {
                apiService.DisconnectWebSocketAsync();
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理个人资料
            MessageBox.Show("个人资料功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrivacyButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理隐私设置
            MessageBox.Show("隐私设置功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理通知设置
            MessageBox.Show("通知设置功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            // 处理关于
            MessageBox.Show("HalloChat v0.2.0-alpha\n安全、高效的即时通讯应用", "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class UserInfo
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class ServerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
        public string Status { get; set; }
    }

    public class ContactInfo
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Status { get; set; }
        public string LastMessage { get; set; }
        public string LastMessageTime { get; set; }
    }

    public class MessageInfo
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }
        public bool IsSelf { get; set; }
    }

    public class GroupInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ContactInfo> Members { get; set; }
    }

    // 转换器类
    public class BooleanToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool boolValue = (bool)value;
            string param = parameter as string;
            
            if (param == "false")
                return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            else
                return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class BooleanToStringConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool boolValue = (bool)value;
            string param = parameter as string;
            
            if (!string.IsNullOrEmpty(param))
            {
                string[] parts = param.Split('|');
                if (parts.Length == 2)
                    return boolValue ? parts[0] : parts[1];
            }
            
            return boolValue.ToString();
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
