using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using MessageLibrary;

namespace Server
{
    public class Server
    {
        public const int port = 9595;
        public static Hashtable users { get; private set; }
        public static String AdminNickname = "Administrator";
        public static BinaryFormatter formatter = new BinaryFormatter();

        public static void Main(String[] args)
        {
            new Server();
        }

        Server()
        {
            users = new Hashtable();
            TcpListener Listener = new TcpListener(IPAddress.Any, port);
            StartListening(Listener);
        }

        private void StartListening(TcpListener Listener)
        {
            Listener.Start();
            while (true)
            {
                while (!Listener.Pending())
                {
                    Thread.Sleep(1000);
                }
                Connection NewConnection = new Connection(Listener);
                ThreadPool.QueueUserWorkItem(new WaitCallback(NewConnection.StartRead));
            }
        }

        public static void RegisterUser(String Nickname, Connection Connection)
        {
            users.Add(Nickname, Connection);
        }

        public static void DeregisterUser(String Nickname)
        {
            users.Remove(Nickname);
        }

        public static void ChangeName(Connection User, String OldNickname, String NewNickname)
        {
            users.Remove(OldNickname);
            users.Add(NewNickname, User);
        }

        public static void SendMessageToAll(Connection from, Message Message)
        {
            foreach (Connection User in users.Values)
            {
                if (User != from)
                {
                    formatter.Serialize(User.Stream, Message);
                }
            }
        }
    }

    public class Connection : IDisposable
    {
        private TcpClient Client;
        public NetworkStream Stream { get; protected set; }
        private static int NumConnections = 0;
        private String Nickname;
        private Boolean isLogged = false;
        private Message MsgToSent = new Message();


        public Connection(TcpListener Listener)
        {
            try
            {
                Client = Listener.AcceptTcpClient();
                Stream = Client.GetStream();
                NumConnections++;
                Console.WriteLine("{0} active connections", NumConnections.ToString());
            }
            catch
            {
                Dispose();
            }
        }

        public void StartRead(Object obj)
        {
            try
            {
                while (true)
                {
                    Message MsgReceived = (Message)Server.formatter.Deserialize(Stream);

                    MsgToSent.cmdCommand = MsgReceived.cmdCommand;
                    MsgToSent.strName = MsgReceived.strName;
                    MsgToSent.strMessage = null;
                    switch (MsgReceived.cmdCommand)
                    {
                        case Command.Login:
                            Server.RegisterUser(MsgReceived.strName, this);
                            isLogged = true;
                            Nickname = MsgReceived.strName;
                            MsgToSent.strMessage = Server.AdminNickname + ": " + MsgReceived.strName + " just entered our char :)";
                            Server.SendMessageToAll(null, MsgToSent);
                            break;
                        case Command.Logout:

                            Dispose();
                            break;
                        case Command.Message:
                            MsgToSent.strMessage = MsgReceived.strName + ": " + MsgReceived.strMessage;
                            Server.SendMessageToAll(this, MsgToSent);
                            break;
                        case Command.Rename:
                            MsgToSent.strMessage = Server.AdminNickname + ": " + MsgReceived.strName + " now knows as " + MsgReceived.strMessage;
                            Server.ChangeName(this, Nickname, MsgReceived.strMessage);
                            Nickname = MsgReceived.strMessage;
                            Server.SendMessageToAll(null, MsgToSent);
                            break;
                        case Command.GetUsersOnline:
                            foreach (String nickname in Server.users.Keys)
                            {
                                MsgToSent.strMessage += nickname + Message.nicknamesDivider;
                            }
                            Server.formatter.Serialize(Stream, MsgToSent);
                            break;
                    }
                }
            }
            catch
            {

                Dispose();
            }
        }

        public void Dispose()
        {
            if (isLogged)
            {
                Server.DeregisterUser(Nickname);
                MsgToSent.strMessage = Server.AdminNickname + ": " + Nickname + " just left us :(";
                Server.SendMessageToAll(null, MsgToSent);
            }

            Stream.Close();
            Client.Close();
            NumConnections--;
            Console.WriteLine("{0} active connections", NumConnections.ToString());
            Thread.CurrentThread.Abort();
        }

    }
}