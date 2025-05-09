using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using AuthClient.Models; // AuthDto.cs 네임스페이스에 맞게 조정

namespace AuthClient
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.10.30:5000") // 실제 서버 IP로 수정
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        // 창 이동
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

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
                    MessageBox.Show("로그인 성공!\n토큰: " + result?.Token.Substring(0, 20) + "...");
                }
                else
                {
                    MessageBox.Show("로그인 실패: 아이디 또는 비밀번호 오류");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류: " + ex.Message);
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

        private void BtnShowRegister_Click(object sender, RoutedEventArgs e)
        {
            AnimateToRegister();
        }

        private void BtnShowLogin_Click(object sender, RoutedEventArgs e)
        {
            AnimateToLogin();
        }

        private void AnimateToRegister()
        {
            RegisterPanel.Visibility = Visibility.Visible;
            var anim1 = new DoubleAnimation(0, -360, TimeSpan.FromMilliseconds(300));
            var anim2 = new DoubleAnimation(360, 0, TimeSpan.FromMilliseconds(300));
            LoginTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim1);
            RegisterTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim2);
        }

        private void AnimateToLogin()
        {
            var anim1 = new DoubleAnimation(-360, 0, TimeSpan.FromMilliseconds(300));
            var anim2 = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(300));
            LoginTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim1);
            RegisterTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim2);
        }
    }
}

