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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using Microsoft.Win32;

namespace AuthClient
{
    public class UserItem : INotifyPropertyChanged
    {
        public string DisplayName { get; set; }
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

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

        public ObservableCollection<UserItem> Users { get; } = new ObservableCollection<UserItem>();
        public ObservableCollection<string> ChatRooms { get; } = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            ChatRooms.Add("Lobby");

            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "대화 보내기" };
            menuItem.Click += MenuChatWithUser_Click;
            contextMenu.Items.Add(menuItem);

            UserListBox.ContextMenu = contextMenu;
            UserListBox.MouseDoubleClick += User_DoubleClick;
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

                    await LoadAllUsers();
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

        private async Task LoadAllUsers()
        {
            try
            {
                var all = await _httpClient.GetFromJsonAsync<List<LoginResponse>>("/api/auth/users");
                Users.Clear();
                foreach (var u in all)
                {
                    Users.Add(new UserItem
                    {
                        DisplayName = $"{u.Department} {u.Name} {u.Position}",
                        IsOnline = false
                    });
                }
            }
            catch { }
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
                    Debug.WriteLine($"[Client ← Server] RAW: '{raw}'");
                    Dispatcher.Invoke(() => HandleServerMessage(raw));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("수신 오류: " + ex.Message));
            }
        }

        private void HandleServerMessage(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;
            if (raw.StartsWith("USERS|"))
            {
                var list = raw.Substring(6).Split(',', StringSplitOptions.RemoveEmptyEntries);
                // Reset all offline
                foreach (var user in Users) user.IsOnline = false;
                // Mark online
                foreach (var name in list)
                {
                    var item = Users.FirstOrDefault(u => u.DisplayName == name);
                    if (item != null) item.IsOnline = true;
                }
            }
            else if (raw.StartsWith("MSG|") || raw.StartsWith("OLD|"))
            {
                bool isOld = raw.StartsWith("OLD|");
                var prefixLength = isOld ? 4 : 4;
                //var parts = raw.Split(new[] { '|' }, 4);
                var parts = raw.Substring(prefixLength).Split(new[] { '|' }, 3);
                if (parts.Length == 3) //4
                {
                    var room = parts[0];
                    var sender = parts[1];
                    var content = parts[2];

                    // 채팅방에 없으면 JOIN 요청 없이 그냥 추가 (중복 방지용)
                    if (!ChatRooms.Contains(room))
                        ChatRooms.Add(room);

                    //  내가 보낸 메시지이고, 이게 실시간이면 무시 (중복 방지)
                    //if (sender == _myDisplayName)
                    //    return;

                    // 실시간 메시지 중 내가 보낸건 무시
                    if (!isOld && sender == _myDisplayName)
                        return;

                    AppendMessage(sender, content, sender == _myDisplayName);
                }
            }

            else if (raw.StartsWith("NEWROOM|"))
            {
                var roomId = raw.Substring("NEWROOM|".Length);
                if (!ChatRooms.Contains(roomId))
                    ChatRooms.Add(roomId);
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
                var roomId = txtCurrentRoom.Text.Trim();
                var message = txtMessage.Text.Trim();

                // 메시지를 MSG|roomId|message 형식으로 보냄
                await _writer.WriteLineAsync($"MSG|{roomId}|{message}");
                await _writer.FlushAsync();

                AppendMessage(_myDisplayName, message, true);
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


        private void BtnShowChat_Click(object sender, RoutedEventArgs e)
        {
            UserListPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
        }

        // 사용자 탭 클릭
        private void BtnShowUsers_Click(object sender, RoutedEventArgs e)
        {
            UserListPanel.Visibility = Visibility.Visible;
            RoomListPanel.Visibility = Visibility.Collapsed;
            SlidePanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Collapsed;
        }

        // 채팅방 탭 클릭
        private void BtnShowChatRooms_Click(object sender, RoutedEventArgs e)
        {
            UserListPanel.Visibility = Visibility.Collapsed;
            RoomListPanel.Visibility = Visibility.Visible;
            SlidePanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Collapsed;
        }

        private void OpenChatPanel(string roomId)
        {
            // 현재 방 텍스트
            txtCurrentRoom.Text = roomId;
            ChatMessages.Children.Clear();

            // 서버에 방 참여 요청 (1:1도 동일)
            _writer?.WriteLineAsync($"JOIN|{roomId}");
            _writer?.FlushAsync();

            // 패널 가시성 설정
            UserListPanel.Visibility = Visibility.Collapsed;
            RoomListPanel.Visibility = Visibility.Collapsed;
            SlidePanel.Visibility = Visibility.Visible;
            ChatPanel.Visibility = Visibility.Visible;

            // 오른쪽 → 왼쪽 슬라이드 애니메이션
            var anim = new DoubleAnimation(360, 0, TimeSpan.FromMilliseconds(300));
            ChatPanelTransform.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        // 우클릭 “대화 보내기” 메뉴
        private void MenuChatWithUser_Click(object sender, RoutedEventArgs e)
        {
            if (UserListBox.SelectedItem is UserItem user)
            {
                // 방 이름을 정렬된 쌍으로 만들기
                var roomId = GetDirectRoomName(_myDisplayName, user.DisplayName);

                if (!ChatRooms.Contains(roomId))
                    ChatRooms.Add(roomId);

                _writer?.WriteLineAsync($"JOIN|{roomId}");
                _writer?.FlushAsync();

                OpenChatPanel(roomId);
            }
        }

        // 더블클릭으로도 동일하게 대화창 열기
        private void User_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 클릭된 ListBoxItem의 DataContext에서 UserItem 꺼내오기
            if ((sender as ListBoxItem)?.DataContext is UserItem user)
            {
                MenuChatWithUser_Click(sender, null);
            }
        }

        private void BtnCreateGroupChat_Click(object sender, RoutedEventArgs e)
        {
            // (간단 예시) 새 방 이름 물어보기
            var roomId = Microsoft.VisualBasic.Interaction.InputBox(
                "새 그룹채팅 방 이름을 입력하세요:", "그룹채팅 생성");
            if (string.IsNullOrWhiteSpace(roomId)) return;

            // 로컬 목록에 추가
            if (!ChatRooms.Contains(roomId))
                ChatRooms.Add(roomId);

            // 서버에 방 참여 요청
            _writer?.WriteLineAsync($"JOIN|{roomId}");
            _writer?.FlushAsync();

            OpenChatPanel(roomId);
        }

        private void ChatRoomsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChatRoomsListBox.SelectedItem is string roomId)
                OpenChatPanel(roomId);
        }

        // 항상 두 사용자 이름 알파벳 순 정렬 -> 방 이름 생성
        private string GetDirectRoomName(string userA, string userB)
        {
            return string.Compare(userA, userB) < 0
                ? $"{userA}_{userB}"
                : $"{userB}_{userA}";
        }
    }
}





