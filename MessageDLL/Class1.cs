using System;
using System.Collections.Generic;
using System.Text;

namespace MessageLibrary
{
    [Serializable]
    public enum Command
    {
        Login,
        Logout,
        GetUsersOnline,
        Message,
        Rename,
        Null
    }

    [Serializable]
    public class Message
    {
        public static char nicknamesDivider = '*';
        public string strName;
        public string strMessage;
        public Command cmdCommand;

        public Message()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }
    }
}