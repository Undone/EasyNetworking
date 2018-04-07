using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetworking;

namespace EasyNetworking.IRC
{
    // https://www.alien.net.au/irc/irc2numerics.html

    public enum IRC_RESPONSE
    {
        NONE = 0,
        WELCOME = 001,
        WHOIS_USER = 311,
        WHOIS_SERVER = 312,
        WHOIS_OPERATOR = 313,
        WHOIS_IDLE = 317,
        WHOIS_END = 318,
        NOTOPIC = 331,
        TOPIC = 332,
        TOPIC_CREATED = 333,
        //BADCHANPASS = 339,
        NAMES = 353,
        NAMES_END = 366,
        MOTD = 372,
        MOTD_START = 375,
        MOTD_END = 376,
        ERROR_UNKNOWN = 400,
        ERROR_NOSUCH_NICK = 401,
        ERROR_NOSUCH_SERVER = 402,
        ERROR_NOSUCH_CHANNEL = 403,
        ERROR_CANNOT_MSG_CHANNEL = 404,
        ERROR_TOO_MANY_CHANNELS = 405,
        ERROR_UNKNOWN_COMMAND = 421,
        ERROR_NICK_CHANGE_DISALLOWED = 430,
        ERROR_NICK_NONE = 431,
        ERROR_NICK_INVALID = 432,
        ERROR_NICK_IN_USE = 433,
        ERROR_NICK_COLLISION = 436,
        ERROR_PASS_INCORRECT = 464,
        ERROR_SERVER_BANNED = 465,
        ERROR_SERVER_BANNED_SOON = 466,
        ERROR_CHANNEL_FULL = 471,
        ERROR_UNKNOWN_MODE = 472,
        ERROR_CHANNEL_INVITE_ONLY = 473,
        ERROR_CHANNEL_BANNED = 474,
        ERROR_CHANNEL_PASSWORD_INVALID = 475,
        ERROR_BAD_PONG = 513
    }

    public enum IRC_CHANNEL_ERROR
    {
        NONE = 0,
        WRONG_PASSWORD,
        FULL,
        BANNED,
        INVITE_ONLY,
        TOO_MANY
    }

    public enum IRC_USER_ACTION
    {
        NONE = 0,
        LEAVE,
        JOIN,
        QUIT,
        MODE
    }

    public struct IRC_User
    {
        public string Name;
        public string Nick;
        public string Host;
    }

    public class IRC_EventArgs : EventArgs
    {
        private string _recpt;

        public IRC_EventArgs(string recpt)
        {
            _recpt = recpt;
        }

        public string Recipient
        {
            get
            {
                return _recpt;
            }
        }

        public IRC_User User { get; set; }
        public string Message { get; set; }
        public bool Succesful { get; set; }
        public int Flag { get; set; }
    }

    public class FrameworkIRC : FrameworkSocket
    {
        private bool _registered;
        private string _nick;

        public event EventHandler Registered;
        public event EventHandler<IRC_EventArgs> MessageReceived;
        public event EventHandler<IRC_EventArgs> ChannelJoin;
        public event EventHandler<IRC_EventArgs> UserModified;

        protected virtual void OnRegistered(object sender, EventArgs e)
        {
            if (Registered != null)
            {
                Registered(sender, e);
            }
        }

        protected virtual void OnMessageReceived(object sender, IRC_EventArgs e)
        {
            if (MessageReceived != null)
            {
                MessageReceived(sender, e);
            }
        }

        protected virtual void OnChannelJoin(object sender, IRC_EventArgs e)
        {
            if (ChannelJoin != null)
            {
                ChannelJoin(sender, e);
            }
        }

        protected virtual void OnUserModified(object sender, IRC_EventArgs e)
        {
            if (UserModified != null)
            {
                UserModified(sender, e);
            }
        }

        public FrameworkIRC(string host, int port = 6667) : base(host, port)
        {
            //SendLine("PASS :connectionpassword");
            SetNick("TestName2222");
            SendLine("USER IRCUser * * :IRCUser");
        }

        public string Nick
        {
            get
            {
                return _nick;
            }
        }

        public void JoinChannel(string channel, string password = "")
        {
            if (password != "")
            {
                channel += " " + password;
            }

            SendLine("JOIN " + channel);
        }

        public void SendAuth(string name, string password)
        {
            if (_registered)
            {
                SendLine("AUTH " + name + " " + password);
            }
        }

        public void SetMode(string user, string mode)
        {
            SendLine("MODE " + user + " " + mode);
        }

        public void SetMode(string mode)
        {
            SetMode(Nick, mode);
        }

        public void SetNick(string nick)
        {
            _nick = nick;

            SendLine("NICK :" + nick);
        }

        public void SendMessage(string recpt, string msg)
        {
            SendLine(string.Format("PRIVMSG {0} :{1}", recpt, msg));
        }

        private string GetCmd(string[] cmd, int index)
        {
            if (cmd.Length < index)
            {
                return "";
            }
            else
            {
                return cmd[index];
            }
        }

        private void HandleData(string data)
        {
            string[] exp = data.Split(' ');
            string[] cmd = exp;

            if (exp.Length > 1)
            {
                if (exp[0].StartsWith(":"))
                {
                    cmd = FrameworkString.SelectRange(exp, 1, exp.Length);
                }
            }

            int nCmd = -1;

            try
            {
                nCmd = Convert.ToInt32(cmd[0]);
            }
            catch (Exception) { }

            if (Enum.IsDefined(typeof(IRC_RESPONSE), nCmd))
            {
                switch ((IRC_RESPONSE)nCmd)
                {
                    case IRC_RESPONSE.WELCOME:
                        {
                            _registered = true;
                            OnRegistered(this, EventArgs.Empty);
                            break;
                        }
                    case IRC_RESPONSE.ERROR_CHANNEL_BANNED:
                        {
                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.Succesful = false;
                            args.Flag = (int)IRC_CHANNEL_ERROR.BANNED;

                            OnChannelJoin(this, args);
                            break;
                        }
                    case IRC_RESPONSE.ERROR_CHANNEL_FULL:
                        {
                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.Succesful = false;
                            args.Flag = (int)IRC_CHANNEL_ERROR.FULL;

                            OnChannelJoin(this, args);
                            break;
                        }
                    case IRC_RESPONSE.ERROR_CHANNEL_PASSWORD_INVALID:
                        {
                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.Succesful = false;
                            args.Flag = (int)IRC_CHANNEL_ERROR.WRONG_PASSWORD;

                            OnChannelJoin(this, args);
                            break;
                        }
                    case IRC_RESPONSE.ERROR_CHANNEL_INVITE_ONLY:
                        {
                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.Succesful = false;
                            args.Flag = (int)IRC_CHANNEL_ERROR.INVITE_ONLY;

                            OnChannelJoin(this, args);
                            break;
                        }
                    case IRC_RESPONSE.ERROR_TOO_MANY_CHANNELS:
                        {
                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.Succesful = false;
                            args.Flag = (int)IRC_CHANNEL_ERROR.TOO_MANY;

                            OnChannelJoin(this, args);
                            break;
                        }
                    case IRC_RESPONSE.NAMES:
                        {
                            break;
                        }
                    case IRC_RESPONSE.NAMES_END:
                        {
                            IRC_EventArgs args = new IRC_EventArgs(cmd[2]);
                            args.Succesful = true;

                            OnChannelJoin(this, args);
                            break;
                        }
                    case IRC_RESPONSE.ERROR_NICK_IN_USE:
                        {
                            SetNick(Nick + "_");
                            break;
                        }
                }
            }
            else
            {
                switch (cmd[0])
                {
                    case "PING":
                        {
                            SendLine("PONG :" + data.Replace("PING :", ""));
                            break;
                        }
                    case "PRIVMSG":
                        {
                            IRC_User user = StringToUser(exp[0]);

                            string[] temp = FrameworkString.SelectRange(cmd, 2, cmd.Length);
                            temp[0] = temp[0].Substring(1, temp[0].Length - 1);

                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.User = user;
                            args.Message = FrameworkString.Concat(temp, " ");

                            OnMessageReceived(this, args);
                            break;
                        }
                    case "JOIN":
                        {
                            IRC_User user = StringToUser(exp[0]);

                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.Flag = (int)IRC_USER_ACTION.JOIN;
                            args.User = user;

                            OnUserModified(this, args);
                            break;
                        }
                    case "PART":
                        {
                            IRC_User user = StringToUser(exp[0]);

                            IRC_EventArgs args = new IRC_EventArgs(cmd[1]);
                            args.Flag = (int)IRC_USER_ACTION.LEAVE;
                            args.User = user;

                            OnUserModified(this, args);
                            break;
                        }
                    case "MODE":
                        {
                            break;
                        }
                    case "NICK":
                        {
                            break;
                        }
                }
            }
        }

        protected override void OnDataReceived(object sender, Socket_EventArgs e)
        {
            string[] data = e.Data.String.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

            foreach (string str in data)
            {
                HandleData(str);
            }

            base.OnDataReceived(sender, e);
        }

        public void Close(string str = "Leaving")
        {
            SendLine("QUIT :" + str);
        }

        public override void Close()
        {
            Close();
            base.Close();
        }

        public static IRC_User StringToUser(string str)
        {
            IRC_User user = new IRC_User();

            str = str.Replace(":", "");

            string[] temp = FrameworkString.Explode(str, "!");
            user.Nick = temp[0];

            temp = FrameworkString.Explode(temp[1], "@");
            user.Name = temp[0];
            user.Host = temp[1];

            return user;
        }
    }
}
