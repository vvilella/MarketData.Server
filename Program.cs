using MarketData.Server.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Timers;
using System.Linq;

namespace MarketData.Server
{
    class Program
    {
        private static Server server;
        private static Dictionary<String, List<Client>> followers = new Dictionary<string, List<Client>>();
        private static System.Timers.Timer timer;
        private static List<ChangePriceModel> ticks = new List<ChangePriceModel>();
        private static DateTime currentDate = DateTime.ParseExact("10/08/2018 10:07:00", "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        static void Main(string[] args)
        {
            loadPrices();
            schedule_Timer();


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

        private static void loadPrices()
        {
            using (var reader = new StreamReader(@"C:\victor\git\tradingBR\MarketData.Server\csv\PETR4_201808101007_201808101654.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var values = reader.ReadLine().Split(';');

                    var date = DateTime.ParseExact(values[0] + values[1], "yyyy.MM.ddHH:mm:ss.fff", CultureInfo.InvariantCulture);

                    Double? ask = String.IsNullOrEmpty(values[2]) ? 0 : Double.Parse(values[2], CultureInfo.InvariantCulture);
                    Double? bid = String.IsNullOrEmpty(values[3]) ? 0 : Double.Parse(values[3], CultureInfo.InvariantCulture);
                    Double? last = String.IsNullOrEmpty(values[4]) ? 0 : Double.Parse(values[4], CultureInfo.InvariantCulture);
                    Int32? volume = String.IsNullOrEmpty(values[5]) ? 0 : Int32.Parse(values[5], CultureInfo.InvariantCulture);

                    var tick = new ChangePriceModel()
                    {
                        Symbol = "PETR4",
                        Date = date,
                        Ask = ask,
                        Bid = bid,
                        Last = last,
                        Volume = volume
                    };

                    ticks.Add(tick);
                }
            }
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
            double tickTime = 1000;
            timer = new Timer(tickTime);
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
        }

        static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(currentDate);
            currentDate = currentDate.AddMilliseconds(1000);

            var selectedTicks = ticks.Where(i => i.Date.Equals(currentDate)).ToList();

            if (selectedTicks.Count > 0)
            {
                var clients = followers["PETR4"];

                foreach (var item in clients)
                {
                    selectedTicks.ForEach(i => server.sendMessageToClient(item, Server.END_LINE + $"{i.ToString()}"));
                }
            }
        }
    }
}