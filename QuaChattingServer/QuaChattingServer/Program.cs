/**************
 * QuartzzDev *
 **************
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace QuaChattingServer
{
    class Program
    {
        static List<TcpClient> clients = new List<TcpClient>();
        static Dictionary<string, TcpClient> clientNames = new Dictionary<string, TcpClient>();
        static bool isAdminMode = false;
        static bool isChatOpen = true;

        static void Main(string[] args)
        {
            Console.WriteLine("Chat Server Başlatılıyor...");
            var server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Server Başlatıldı. Bağlantılar bekleniyor...");

            Task.Run(() => HandleAdminCommands());

            while (true)
            {
                var client = server.AcceptTcpClient();
                clients.Add(client);
                Console.WriteLine("Yeni bir kullanıcı bağlandı.");
                Task.Run(() => HandleClient(client));
            }
        }

        static void HandleAdminCommands()
        {
            while (true)
            {
                string adminCommand = Console.ReadLine();
                if (adminCommand == "/admin")
                {
                    isAdminMode = true;
                    Console.WriteLine("Admin modu etkinleştirildi.");
                    while (isAdminMode)
                    {
                        string command = Console.ReadLine();
                        if (command == "/msil")
                        {
                            ClearMessages();
                        }
                        else if (command.StartsWith("/kullaniciat"))
                        {
                            string[] parts = command.Split(' ');
                            if (parts.Length == 2)
                            {
                                KickUser(parts[1]);
                            }
                        }
                        else if (command == "/sohbetac")
                        {
                            isChatOpen = true;
                            BroadcastMessage("Sohbet açıldı.");
                        }
                        else if (command == "/sohbetkapat")
                        {
                            isChatOpen = false;
                            BroadcastMessage("Sohbet kapatıldı.");
                        }
                        else if (command == "/admin")
                        {
                            isAdminMode = false;
                            Console.WriteLine("Admin modu devre dışı bırakıldı.");
                        }
                    }
                }
            }
        }

        static void ClearMessages()
        {
            Console.Clear();
            BroadcastMessage("Mesajlar silindi.");
            Console.WriteLine("Mesajlar silindi.");
        }

        static void KickUser(string userName)
        {
            if (clientNames.ContainsKey(userName))
            {
                TcpClient client = clientNames[userName];
                clients.Remove(client);
                clientNames.Remove(userName);
                client.Close();
                Console.WriteLine($"{userName} odadan atıldı.");
                BroadcastMessage($"{userName} odadan atıldı.");
            }
            else
            {
                Console.WriteLine($"{userName} bulunamadı.");
            }
        }

        static void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            string userName = null;

            // Kullanıcıdan isim al
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                userName = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                if (!string.IsNullOrEmpty(userName) && !clientNames.ContainsKey(userName))
                {
                    clientNames.Add(userName, client);
                    SendWelcomeMessage(client, userName);
                    BroadcastMessage($"Server DUYURU: {userName} aramıza katıldı!");
                    Console.WriteLine($"{userName} aramıza katıldı!");
                    break;
                }
                else
                {
                    byte[] error = Encoding.UTF8.GetBytes("Bu isim zaten kullanılıyor. Başka bir isim seçiniz.");
                    stream.Write(error, 0, error.Length);
                }
            }

            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message);

                    if (isChatOpen || message.StartsWith("Server DUYURU:"))
                    {
                        // Mesajı diğer clientlara gönderelim
                        BroadcastMessage(message, client);
                    }
                    else
                    {
                        byte[] response = Encoding.UTF8.GetBytes("Sohbet kapalı.");
                        stream.Write(response, 0, response.Length);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            clients.Remove(client);
            clientNames.Remove(userName);
            client.Close();
            Console.WriteLine("Client bağlantısı kapatıldı.");
        }

        static void SendWelcomeMessage(TcpClient client, string userName)
        {
            string[] welcomeMessages = {
                "Hoş geldin, {0}!",
                "Merhaba {0}, sohbetimize hoş geldin!",
                "{0} aramıza katıldı!"
            };

            Random rand = new Random();
            string selectedMessage = string.Format(welcomeMessages[rand.Next(welcomeMessages.Length)], userName);
            byte[] buffer = Encoding.UTF8.GetBytes($"Server DUYURU: {selectedMessage}");
            client.GetStream().Write(buffer, 0, buffer.Length);
        }

        static void BroadcastMessage(string message, TcpClient excludeClient = null)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                if (client != excludeClient)
                {
                    try
                    {
                        client.GetStream().Write(buffer, 0, buffer.Length);
                    }
                    catch (Exception)
                    {
                        // Client yazılamıyorsa hata alındıysa clienti kaldır
                        clients.Remove(client);
                    }
                }
            }
        }
    }
}
