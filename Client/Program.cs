using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using MessageLibrary;

namespace Client
{
    class Client
    {
        public const int port = 9595;
        public IPAddress IP = IPAddress.Parse("127.0.0.1");
        public BinaryFormatter binaryFmt = new BinaryFormatter();


        private static String welcomeMessage = "Welcome, use \"!help\" to view a list of commands";
        private static String helpMessage = "Commands:\n" +
                                            "\t!users - to view a list of users online\n" +
                                            "\t!rename <NickName> - to rename ur nickname\n" +
                                            "\t!help - view a program help\n" +
                                            "\t!exit - to exit program";

        private const int timeBetweenReconnectsInMillies = 3000;
        private NetworkStream Stream;

        public static void Main(String[] args)
        {
            new Client();
        }

        Client()
        {
            TcpClient TcpClient = new TcpClient();

        tryConnect:
            try
            {
                TcpClient.Connect(IP, port);
                Stream = TcpClient.GetStream();
                Thread thrMessaging = new Thread(new ThreadStart(ReceiveMessages));
                thrMessaging.IsBackground = true;
                thrMessaging.Start();
                SendMessages(); //TODO rename it
            }
            catch
            {
                Console.WriteLine("[X] Server not available!");
                Thread.Sleep(timeBetweenReconnectsInMillies);
                goto tryConnect;
            }
        }

        private void SendMessages()
        {
            String Nickname;
            String InputText;
            Message MsgToSend = new Message();

            try
            {
                Console.WriteLine("Inter your nickname:");
                Nickname = Console.ReadLine();
                MsgToSend.cmdCommand = Command.Login;
                MsgToSend.strName = Nickname;
                MsgToSend.strMessage = null;
                binaryFmt.Serialize(Stream, MsgToSend);
                ShowWelcomeMessage();

                while (true)
                {
                    Console.WriteLine(Nickname + ":");
                    InputText = Console.ReadLine();
                    if (!InputText.Equals(""))
                    {
                        if (InputText.StartsWith("!"))
                        {
                            if (InputText.Equals("!users"))
                            {
                                MsgToSend.cmdCommand = Command.GetUsersOnline;
                                MsgToSend.strMessage = null;
                                MsgToSend.strName = null;

                                binaryFmt.Serialize(Stream, MsgToSend);
                            }
                            else if (InputText.Equals("!help"))
                            {
                                ShowHelpMessage();
                            }
                            else if (InputText.Equals("!exit"))
                            {
                                MsgToSend.cmdCommand = Command.Logout;
                                MsgToSend.strName = Nickname;
                                MsgToSend.strMessage = null;

                                binaryFmt.Serialize(Stream, MsgToSend);
                                Environment.Exit(1);
                            }
                            else if (InputText.StartsWith("!rename"))
                            {
                                String[] splitted = InputText.Split(' ');
                                if (splitted.Length == 2 && splitted[1].Length < 10)
                                {
                                    MsgToSend.strName = Nickname;
                                    MsgToSend.cmdCommand = Command.Rename;
                                    MsgToSend.strMessage = splitted[1];

                                    binaryFmt.Serialize(Stream, MsgToSend);
                                    Nickname = splitted[1];
                                }
                                else
                                {
                                    Console.WriteLine("Usage: !rename <NewNickname>");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Unknown command");
                            }
                        }
                        else
                        {
                            MsgToSend.cmdCommand = Command.Message;
                            MsgToSend.strName = Nickname;
                            MsgToSend.strMessage = InputText;
                            binaryFmt.Serialize(Stream, MsgToSend);
                        }
                    }
                }
            }
            catch
            {
                Stream.Close();
                Console.WriteLine("Check ur internet connection");
                Console.ReadLine();
            }
        }

        private static void ShowWelcomeMessage()
        {
            Console.WriteLine(welcomeMessage);
        }

        private static void ShowHelpMessage()
        {
            Console.WriteLine(helpMessage);
        }

        private void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    Message MsgReceived = (Message)binaryFmt.Deserialize(Stream);

                    switch (MsgReceived.cmdCommand)
                    {
                        case Command.Message:
                        case Command.Login:
                        case Command.Logout:
                        case Command.Rename:
                            Console.WriteLine(MsgReceived.strMessage);
                            break;
                        case Command.GetUsersOnline:
                            string[] names = MsgReceived.strMessage.Split('*');
                            Console.WriteLine("Users online:\n");
                            foreach (String name in names)
                            {
                                Console.WriteLine(name);
                            }
                            break;
                    }
                }
            }
            catch
            {
                Thread.CurrentThread.Abort();
            }

        }
    }
}