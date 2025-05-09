using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;

        public MainWindow()
        {
            InitializeComponent();
            ConnectAsync();
        }

        private async void ConnectAsync()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync("192.168.10.30", 12345);
                stream = client.GetStream();
                _ = ReceiveLoop();
                AppendText("Connected to server.\n");
            }
            catch (Exception ex)
            {
                AppendText($"Connection error: {ex.Message}\n");
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            try
            {
                while (true)
                {
                    int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteCount <= 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Dispatcher.Invoke(() => AppendText(msg + "\n"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendText($"Receive error: {ex.Message}\n"));
            }
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(TxtInput.Text) || stream == null) return;

            string message = TxtInput.Text.Trim();
            byte[] data = Encoding.UTF8.GetBytes(message);
            try
            {
                stream.Write(data, 0, data.Length);
                AppendText($"Me: {message}\n");
            }
            catch (Exception ex)
            {
                AppendText($"Send error: {ex.Message}\n");
            }
            finally
            {
                TxtInput.Clear();
                TxtInput.Focus();
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendMessage();
        }

        private void AppendText(string text)
        {
            TxtChat.AppendText(text);
            TxtChat.ScrollToEnd();
        }
    }
}