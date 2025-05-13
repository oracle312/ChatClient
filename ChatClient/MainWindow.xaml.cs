using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Net.Sockets;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using AuthClient.Models;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace AuthClient
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.10.30:5000")
        };

        private TcpClient? _chatClient;
        private StreamWriter? _writer;
        private StreamReader? _reader;
        private string _myDisplayName = "나";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginReq = new LoginRequest
            {
                Username = txtLoginId.Text,
                Password = txtLoginPw.Password
            };

            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/auth/login", loginReq);
                if (res.IsSuccessStatusCode)
                {
                    var result = await res.Content.ReadFromJsonAsync<LoginResponse>();
                    _myDisplayName = $"{result.Department} {result.Name} {result.Position}";

                    MessageBox.Show($"로그인 성공!\n환영합니다, {_myDisplayName}님");
                    LoginPanel.Visibility = Visibility.Collapsed;
                    ChatPanel.Visibility = Visibility.Visible;

                    await ConnectToChatServer();
                }
                else
                {
                    MessageBox.Show("로그인 실패: 아이디 또는 비밀번호가 올바르지 않습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류: " + ex.Message);
            }
        }

        private async Task ConnectToChatServer()
        {
            try
            {
                _chatClient = new TcpClient();
                _chatClient.NoDelay = true;
                await _chatClient.ConnectAsync("192.168.10.30", 12345);

                var stream = _chatClient.GetStream();
                _writer = new StreamWriter(stream) { AutoFlush = true };
                _reader = new StreamReader(stream);

                await _writer.WriteLineAsync("AUTH|" + _myDisplayName);
                await _writer.FlushAsync();

                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                MessageBox.Show("채팅 서버 연결 실패: " + ex.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (_chatClient?.Connected == true)
                {
                    var raw = await _reader!.ReadLineAsync();
                    Debug.WriteLine($"[Client ← Server] RAW: \"{raw}\"");
                    if (!string.IsNullOrEmpty(raw) && raw.StartsWith("MSG|"))
                    {
                        var parts = raw.Split(new[] { '|' }, 3);
                        if (parts.Length == 3)
                        {
                            var sender = parts[1];
                            var content = parts[2];
                            Dispatcher.Invoke(() => AppendMessage(sender, content, sender == _myDisplayName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("수신 오류: " + ex.Message));
            }
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var signupReq = new SignupRequest
            {
                Username = txtUsername.Text,
                Password = txtPassword.Password,
                Name = txtName.Text,
                Position = txtPosition.Text,
                Department = txtDepartment.Text,
                Email = txtEmail.Text
            };

            try
            {
                var res = await _httpClient.PostAsJsonAsync("/api/auth/signup", signupReq);
                if (res.IsSuccessStatusCode)
                {
                    MessageBox.Show("회원가입 성공! 로그인으로 이동합니다.");
                    AnimateToLogin();
                }
                else
                {
                    var error = await res.Content.ReadAsStringAsync();
                    MessageBox.Show("회원가입 실패: " + error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류: " + ex.Message);
            }
        }

        private void BtnShowRegister_Click(object sender, RoutedEventArgs e) => AnimateToRegister();

        private void BtnShowLogin_Click(object sender, RoutedEventArgs e) => AnimateToLogin();

        private void AnimateToRegister()
        {
            RegisterPanel.Visibility = Visibility.Visible;
            var anim1 = new DoubleAnimation(0, -360, TimeSpan.FromMilliseconds(300));
            var anim2 = new DoubleAnimation(360, 0, TimeSpan.FromMilliseconds(300));
            LoginTransform.BeginAnimation(TranslateTransform.XProperty, anim1);
            RegisterTransform.BeginAnimation(TranslateTransform.XProperty, anim2);
        }

        private void AnimateToLogin()
        {
            var anim1 = new DoubleAnimation(-360, 0, TimeSpan.FromMilliseconds(300));
            var anim2 = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(300));
            LoginTransform.BeginAnimation(TranslateTransform.XProperty, anim1);
            RegisterTransform.BeginAnimation(TranslateTransform.XProperty, anim2);
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (_writer != null && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                await _writer.WriteAsync(txtMessage.Text + "\n");
                await _writer.FlushAsync();
                AppendMessage(_myDisplayName, txtMessage.Text, true);
                txtMessage.Clear();
            }
        }

        private void ScrollToBottom() => ChatScroll.ScrollToEnd();

        private void AppendMessage(string sender, string message, bool isMine = false)
        {
            var time = DateTime.Now.ToString("HH:mm");

            var senderText = new TextBlock
            {
                Text = sender,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkGray,
                Margin = new Thickness(5, 0, 5, 2),
                HorizontalAlignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var text = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 200,
                Margin = new Thickness(5)
            };

            var bubble = new Border
            {
                Background = isMine ? Brushes.LightYellow : Brushes.LightGray,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Child = text,
                HorizontalAlignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var timeText = new TextBlock
            {
                Text = time,
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(5, 0, 5, 10),
                HorizontalAlignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(10),
                HorizontalAlignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            panel.Children.Add(senderText);
            panel.Children.Add(bubble);
            panel.Children.Add(timeText);

            ChatMessages.Children.Add(panel);
            ScrollToBottom();
        }
    }
}



