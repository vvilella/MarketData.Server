using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Timers;

namespace MarketData.Server
{
    class Program
    {
        private static Server server;
        private static Dictionary<String, List<Client>> followers = new Dictionary<string, List<Client>>();
        private static System.Timers.Timer timer;

        static void Main(string[] args)
        {
            server = new Server(IPAddress.Any);
            server.ClientConnected += clientConnected;
            server.ClientDisconnected += clientDisconnected;
            server.ConnectionBlocked += connectionBlocked;
            server.MessageReceived += messageReceived;
            server.start();

            Console.WriteLine("SERVER STARTED: " + DateTime.Now);

            char read = Console.ReadKey(true).KeyChar;

            do
            {
                if (read == 'b')
                {
                    server.sendMessageToAll(Console.ReadLine());
                }
            } while ((read = Console.ReadKey(true).KeyChar) != 'q');

            server.stop();
        }

        private static void clientConnected(Client client)
        {
            Console.WriteLine("CONNECTED: " + client);

            server.sendMessageToClient(client, "Marketdata Server" + Server.END_LINE + "Login: ");
        }

        private static void clientDisconnected(Client client)
        {
            Console.WriteLine("DISCONNECTED: " + client);
        }

        private static void connectionBlocked(IPEndPoint endpoint)
        {
            Console.WriteLine(string.Format("BLOCKED: {0}:{1} at {2}", endpoint.Address, endpoint.Port, DateTime.Now));
        }

        private static void messageReceived(Client client, string message)
        {
            if (client.getCurrentStatus() != ClientStatus.LoggedIn)
            {
                handleLogin(client, message);
                return;
            }

            Console.WriteLine("MESSAGE: " + message);

            if (message == "logout" || message == "exit")
            {
                server.kickClient(client);
                server.sendMessageToClient(client, Server.END_LINE + Server.CURSOR);
            }
            else if (message == "clear")
            {
                server.clearClientScreen(client);
                server.sendMessageToClient(client, Server.CURSOR);
            }
            else if (message.StartsWith("follow"))
            {
                var symbol = extractSymbol(message);

                if (isValidSymbol(symbol))
                {
                    addFollower(symbol, client);
                    server.sendMessageToClient(client, Server.END_LINE + "Seguindo: " + symbol);
                }
            }

            server.sendMessageToClient(client, Server.END_LINE + Server.CURSOR);
        }

        private static void addFollower(string symbol, Client client)
        {
            schedule_Timer();

            if (followers.ContainsKey(symbol))
            {
                followers[symbol].Add(client);
            }
            else
            {
                var clientList = new List<Client>();
                clientList.Add(client);
                followers.Add(symbol, clientList);
            }
        }

        private static bool isValidSymbol(string symbol)
        {
            return (!String.IsNullOrEmpty(symbol));
        }

        private static String extractSymbol(string message)
        {
            var values = message.Split(' ');
            if (values.Length == 2)
                return values[1].ToString().ToUpper();
            else
                return null;
        }

        private static void handleLogin(Client client, string message)
        {
            ClientStatus status = client.getCurrentStatus();

            if (status == ClientStatus.Guest)
            {
                if (message == "root")
                {
                    server.sendMessageToClient(client, Server.END_LINE + "Password: ");
                    client.setStatus(ClientStatus.Authenticating);
                }

                else
                    server.kickClient(client);
            }

            else if (status == ClientStatus.Authenticating)
            {
                if (message == "r00t")
                {
                    server.clearClientScreen(client);
                    server.sendMessageToClient(client, "Successfully authenticated." + Server.END_LINE + Server.CURSOR);
                    client.setStatus(ClientStatus.LoggedIn);
                }

                else
                    server.kickClient(client);
            }
        }

        static void schedule_Timer()
        {
            double tickTime = 2300;
            timer = new System.Timers.Timer(tickTime);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
        }

        static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            var clients = followers["PETR4"];
            var date = DateTime.Now;
            var price = "12.52";

            foreach (var item in clients)
            {
                server.sendMessageToClient(item, $"{Server.END_LINE}SYMBOL: PETR4 - DATE: {date.ToString("yyyy-MM-dd HH:mm:ss:FFF")} - PRICE: {price}");
            }

            schedule_Timer();
        }
    }
}