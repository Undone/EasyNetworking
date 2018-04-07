using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EasyNetworking
{
    public class FrameworkListener
    {
        public event EventHandler<Socket_EventArgs> ClientConnected;
        public event EventHandler<Socket_EventArgs> ClientDisconnected;
        public event EventHandler<Socket_EventArgs> DataReceived;

        protected virtual void OnClientConnected(object sender, Socket_EventArgs e)
        {
            if (ClientConnected != null)
            {
                ClientConnected(sender, e);
            }
        }

        protected virtual void OnClientDisconnected(object sender, Socket_EventArgs e)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(sender, e);
            }
        }

        protected virtual void OnDataReceived(object sender, Socket_EventArgs e)
        {
            if (DataReceived != null)
            {
                DataReceived(sender, e);
            }
        }

        private List<FrameworkSocket> clientList;
        private TcpListener listener;
        private Thread listenThread;
        private bool shouldListen;
        private IPAddress listenAddr;
        private int listenPort;

        public FrameworkListener(int port, string address = "127.0.0.1")
        {
            clientList = new List<FrameworkSocket>();

            try
            {
                IPAddress addr = IPAddress.Parse(address);

                listenAddr = addr;
                listenPort = port;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public int NumClients
        {
            get
            {
                return clientList.Count;
            }
        }

        public void StartListener()
        {
            shouldListen = true;

            listener = new TcpListener(listenAddr, listenPort);
            listener.Start();

            listenThread = new Thread(new ThreadStart(ListenConnections));
            listenThread.Start();
        }

        public void StopListener()
        {
            shouldListen = false;

            listener.Stop();

            //listenThread.Abort();
            listenThread.Join();
        }

        private void ListenConnections()
        {
            while (shouldListen)
            {
                FrameworkSocket client = new FrameworkSocket(listener.AcceptTcpClient());
                client.DataReceived += OnDataReceived;
                client.Disconnected += OnClientDisconnected;

                clientList.Add(client);
                OnClientConnected(this, new Socket_EventArgs(client));
            }
        }
    }
}
