/**************
 * QuartzzDev *
 **************
*/

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace QuaChattingApp
{
    class Program
    {
        static bool isChatOpen = true;

        static void Main(string[] args)
        {
            Console.Write("Kullanıcı adı : ");
            string name = Console.ReadLine();

            Console.WriteLine("Chat Client Başlatılıyor...");
            var client = new TcpClient("server_ip", 5000);
            var stream = client.GetStream();

            // İsmi gönder
            SendInitialMessage(stream, name);

            // Mesaj gönderme işlemini asenkron olarak çalıştır
            Task.Run(() => SendMessage(stream, name));

            var buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message);

                    if (message == "Sohbet kapalı.")
                    {
                        isChatOpen = false;
                    }
                    else if (message == "Sohbet açıldı.")
                    {
                        isChatOpen = true;
                    }
                    else if (message == "Mesajlar silindi.")
                    {
                        Console.Clear();
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            client.Close();
            Console.WriteLine("Server bağlantısı kapatıldı.");
        }

        static void SendInitialMessage(NetworkStream stream, string name)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(name);
            stream.Write(buffer, 0, buffer.Length);
        }

        static void SendMessage(NetworkStream stream, string name)
        {
            while (true)
            {
                if (!isChatOpen)
                {
                    Console.WriteLine("Sohbet kapalı, mesaj gönderemezsiniz.");
                    continue;
                }

                string message = Console.ReadLine();
                byte[] buffer = Encoding.UTF8.GetBytes($"{name}: {message}");
                stream.Write(buffer, 0, buffer.Length);
            }
        }
    }
}
