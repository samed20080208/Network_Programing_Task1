using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;

namespace Client;

public partial class MainWindow : Window
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _serverEndpoint;
    private DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();
        _udpClient = new UdpClient();
        _serverEndpoint = new IPEndPoint(IPAddress.Loopback, 27001);
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            btnStart.IsEnabled = false;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += async (s, args) => await UpdateScreenshot();
            _timer.Start();

            await UpdateScreenshot();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            btnStart.IsEnabled = true;
        }
    }

    private async Task UpdateScreenshot()
    {
        var receivedBuffer = new byte[ushort.MaxValue - 29];
        await _udpClient.SendAsync(receivedBuffer, receivedBuffer.Length, _serverEndpoint);

        var list = new List<byte>();
        try
        {
            while (true)
            {
                var result = await _udpClient.ReceiveAsync();
                var receivedData = result.Buffer;
                list.AddRange(receivedData);

                if (receivedData.Length < receivedBuffer.Length) break;
            }

            try
            {
                var image = ByteArrayToImage([.. list]);
                ImageBox.Source = image;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error converting byte array to image: {ex.Message}");
                _timer.Stop();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error receiving data: {ex.Message}");
            _timer.Stop();
        }
    }

    private BitmapImage ByteArrayToImage(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length == 0) return null;

        var image = new BitmapImage();
        using (var m = new MemoryStream(byteArray))
        {
            m.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = m;
            image.EndInit();
        }
        image.Freeze();
        return image;
    }
}