using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace EasyNetworking
{
    public enum CONNECTION_FLAG
    {
        
    }

    // System.Net.NetException errorcode
    // 10060 timeout
    // 10061 actively refused

    public class Socket_Data
    {
        private byte[] bytes;

        public Socket_Data(byte[] data)
        {
            bytes = data;
        }

        public byte[] Bytes
        {
            get
            {
                return bytes;
            }
        }

        public string String
        {
            get
            {
                return Encoding.UTF8.GetString(bytes);
            }
        }

        public long Size
        {
            get
            {
                return bytes.LongLength;
            }
        }
    }

    public class Socket_EventArgs : EventArgs
    {
        private FrameworkSocket sock;

        public Socket_EventArgs(FrameworkSocket s)
        {
            sock = s;
        }

        public FrameworkSocket Socket
        {
            get
            {
                return sock;
            }
        }

        public Socket_Data Data { get; set; }
    }

    public class Connection_EventArgs : EventArgs
    {
        public bool Success;
    }

    public class FrameworkSocket
    {
        private TcpClient sock;
        private string host;
        private bool shouldQueue;
        private bool shouldRead;

        public event EventHandler<Socket_EventArgs> DataSent;
        public event EventHandler<Socket_EventArgs> DataReceived;
        public event EventHandler<Socket_EventArgs> Disconnected;

        protected virtual void OnDataSent(object sender, Socket_EventArgs e)
        {
            DataSent?.Invoke(sender, e);
        }

        protected virtual void OnDataReceived(object sender, Socket_EventArgs e)
        {
            DataReceived?.Invoke(sender, e);
        }

        protected virtual void OnDisconnected(object sender, Socket_EventArgs e)
        {
            Disconnected?.Invoke(sender, e);
        }

        // retry interval, timeout

        public FrameworkSocket(string host, int port)
        {
            try
            {
                sock = new TcpClient(host, port);
                this.host = host;

                Task.Factory.StartNew(Read, TaskCreationOptions.LongRunning);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public FrameworkSocket(TcpClient client)
        {
            sock = client;
            host = client.Client.RemoteEndPoint.ToString();

            Task.Factory.StartNew(Read, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Should the FrameworkSocket queue data for sending it later, if it fails to send it now
        /// </summary>
        public bool QueueData
        {
            get
            {
                return shouldQueue;
            }

            set
            {
                shouldQueue = value;
            }
        }

        /// <summary>
        /// Returns true if the FrameworkSocket is ready to read data
        /// </summary>
        public bool Active
        {
            get
            {
                return shouldRead;
            }
        }

        public string Host
        {
            get
            {
                return host;
            }
        }

        public void Send(byte[] buffer)
        {
            if (sock.Connected && sock.GetStream().CanWrite)
            {
                sock.GetStream().Write(buffer, 0, buffer.Length);
                sock.GetStream().Flush();

                Socket_EventArgs args = new Socket_EventArgs(this);
                args.Data = new Socket_Data(buffer);

                OnDataSent(this, args);
            }
            else
            {
                if (QueueData)
                {

                }
                else
                {
                    //throw new Exception("Socket stream not available for writing");
                }
            }
        }

        public void Send(string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);

            Send(buffer);
        }

        public void SendLine(string str)
        {
            Send(str + Environment.NewLine);
        }

        private async Task Read()
        {
            while (shouldRead)
            {
                if (sock.Connected && sock.GetStream().CanRead)
                {
                    List<byte[]> byteList = new List<byte[]>();

                    while (sock.GetStream().DataAvailable)
                    {
                        byte[] buffer = new byte[1024];
                        int read = await sock.GetStream().ReadAsync(buffer, 0, buffer.Length);

                        byteList.Add(buffer);
                    }

                    if (byteList.Count > 0)
                    {
                        byte[] data = byteList.SelectMany(x => x).ToArray();

                        Socket_EventArgs args = new Socket_EventArgs(this);
                        args.Data = new Socket_Data(data);

                        OnDataReceived(this, args);
                        byteList.Clear();
                    }
                }

                await Task.Delay(1000);
            }
        }

        public virtual void Close()
        {
            shouldRead = false;
            OnDisconnected(this, new Socket_EventArgs(this));
            sock.Client.Close();
        }
    }
}
