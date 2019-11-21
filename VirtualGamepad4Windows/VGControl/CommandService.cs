using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualGamepad4Windows
{
    public class CommandService
    {
        /// Server socket
        private Socket _serverSocket;

        /// Element for sync wait 
        private static ManualResetEvent _connectionMutex =
                 new ManualResetEvent(false);

        /// Client handler
        private ClientManager _clientManager;
        /// List of client connections
        private List<Socket> _clientConnections = new List<Socket>();
        private BackgroundWorker _listenThread = new BackgroundWorker();


        public CommandService(string ipAddrees, int port)
        {
            try
            {
                this._serverSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                this._serverSocket.Bind(
                  new IPEndPoint(IPAddress.Parse(ipAddrees), port));

            }
            catch (Exception ex)
            {
                throw new Exception("Server Init Error.", ex);
            }
        }

       

        public void Start()
        {
            this._clientManager = new ClientManager(this._clientConnections);

            this._listenThread.WorkerReportsProgress = true;
            this._listenThread.WorkerSupportsCancellation = true;
            this._listenThread.DoWork +=
                 new DoWorkEventHandler(ListenThread_DoWork);

            this._listenThread.RunWorkerAsync(this._serverSocket);
        }

        /// Thread for listening port
        private void ListenThread_DoWork(object sender, DoWorkEventArgs e)
        {
            Socket serverSocket = (Socket)e.Argument;

            serverSocket.Listen(100);

            while (true)
            {
                // reset mutex
                _connectionMutex.Reset();

                serverSocket.BeginAccept(
                new AsyncCallback(this.AcceptCallback), this._serverSocket);

                // waiting for the next connection
                _connectionMutex.WaitOne();
            }
        }



        public int ConnectionsCount
        {
            get { return this._clientConnections.Count; }
        }

        /// Callback method for handling connections
        private void AcceptCallback(IAsyncResult asyncResult)
        {
            _connectionMutex.Set();

            Socket serverSocket = (Socket)asyncResult.AsyncState;
            Socket clientSocket = (Socket)serverSocket.EndAccept(asyncResult);
            this._clientConnections.Add(clientSocket);

            this._clientManager.HandleClient(clientSocket);
        }
    }


    public class ClientManager
    {

        private List<BackgroundWorker> _clientProcessors = new List<BackgroundWorker>();


        private List<Socket> _connections;

        public ClientManager(List<Socket> connections)
        {
            this._connections = connections;
        }

        /// Handling of client connection      
        public void HandleClient(Socket clientSocket)
        {
            BackgroundWorker clientProcessor = new BackgroundWorker();
            clientProcessor.DoWork += new DoWorkEventHandler(ClientProcessing);

            this._clientProcessors.Add(clientProcessor);

            List<Socket> args = new List<Socket>();
            // 
            // args.Add(...);           

            clientProcessor.RunWorkerAsync(args);
        }

        private void ClientProcessing(object sender, DoWorkEventArgs e)
        {
            // reading args
            List<Socket> args = (List<Socket>)e.Argument;

            //ProtocolSerializer serializer = new ProtocolSerializer();
            foreach (Socket socket in args)
            {
                try
                {
                    while (socket.Connected)
                    {
                        // ...
                        Console.WriteLine("Conected");

                    }
                }
                catch (SocketException)
                {
                    // ...
                }
                catch (Exception)
                {
                    // ...
                }
            }
        }
    }
}
