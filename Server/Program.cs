using System.Net.Sockets;
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

class Program
{
    static async Task Main()
    {
        using UdpClient udpServer = new(27001);
        IPEndPoint localEndpoint = new(IPAddress.Parse("192.168.1.8"), 27001);
        Console.WriteLine($"Server started on {localEndpoint}");

        while (true)
        {
            UdpReceiveResult result = await udpServer.ReceiveAsync();

            while (true)
            {
                Bitmap screenshot = GetScreenshot();
                byte[] imageData = ImageToByteArray(screenshot);

                if (imageData.Length / 1024f >= 1000)
                    Console.WriteLine($"{(imageData.Length / 1024f) / 1024f} mb");
                else
                    Console.WriteLine($"{imageData.Length / 1024f} kb");

                var chunk = imageData.Chunk(ushort.MaxValue - 29);
                var buffer = chunk.ToArray();

                for (int i = 0; i < buffer.Length; i++)
                {
                    await Task.Delay(30);
                    await udpServer.SendAsync(buffer[i], buffer[i].Length, result.RemoteEndPoint);
                }

                Console.WriteLine($"Sent screenshot to {result.RemoteEndPoint}");
                Console.WriteLine();

                await Task.Delay(2000);
            }

        }
    }

    static Bitmap GetScreenshot()
    {
        Bitmap? screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,Screen.PrimaryScreen.Bounds.Height)!;

        Graphics graphics = Graphics.FromImage(screenshot);
        graphics.CopyFromScreen(0, 0, 0, 0, screenshot.Size);

        return screenshot;
    }

    static byte[] ImageToByteArray(Image image)
    {
        using (MemoryStream stream = new())
        {
            image.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }
}

