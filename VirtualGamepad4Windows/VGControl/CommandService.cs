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
            //启动监听，并且设置一个最大的队列长度
            serverSocket.Listen(100);

            while (true)
            {
                // reset mutex
                _connectionMutex.Reset();

                serverSocket.BeginAccept(new AsyncCallback(this.ClientAcceptedCallback), this._serverSocket);

                // waiting for the next connection
                _connectionMutex.WaitOne();
            }
        }



        public int ConnectionsCount
        {
            get { return this._clientConnections.Count; }
        }

        /// 回调客户端连入方法
        private void ClientAcceptedCallback(IAsyncResult asyncResult)
        {
            _connectionMutex.Set();

            Socket serverSocket = (Socket)asyncResult.AsyncState;
            if (serverSocket != null)
            {
                Socket clientSocket = (Socket)serverSocket.EndAccept(asyncResult);
                this._clientConnections.Add(clientSocket);
                this._clientManager.HandleClient(clientSocket);
            }
        }

    }


    public class ClientManager
    {

        private List<BackgroundWorker> _clientProcessors = new List<BackgroundWorker>();

        private static readonly byte[] _socketBuffer = new byte[1024];
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
            clientSocket.BeginReceive(_socketBuffer, 0, _socketBuffer.Length, SocketFlags.None, new AsyncCallback(AsyncReveiveMessage), clientSocket);
            args.Add(clientSocket);
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
                    if (socket.Connected)
                    {
                        // ...
                        Console.WriteLine("Conected");
                        //socket.Receive
                        //AsyncReveive(socket);
   
                    }
                }
                catch (SocketException se)
                {
                    // ...
                    Console.WriteLine("SocketException:"+se.Message);
                }
                catch (Exception ex)
                {
                    // ...
                    Console.WriteLine("Exception:" + ex.Message);
                }
            }
        }
        private void AsyncReveiveMessage(IAsyncResult ar)
        {
            byte[] data = new byte[1024];
            var socket = ar.AsyncState as Socket;
            //客户端IP地址和端口信息
            if (socket != null)
            {
                IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                try
                {
                    //方法参考：http://msdn.microsoft.com/zh-cn/library/system.net.sockets.socket.endreceive.aspx
                    var length = socket.EndReceive(ar);
                    //读取出来消息内容
                    var message = Encoding.UTF8.GetString(_socketBuffer, 0, length);
                    //输出接收信息
                    Console.WriteLine(clientipe + " ：" + message, ConsoleColor.White);
                    //服务器发送消息
                    socket.Send(Encoding.UTF8.GetBytes("Server received data"));
                    //接收下一个消息
                    socket.BeginReceive(_socketBuffer, 0, _socketBuffer.Length, SocketFlags.None, new AsyncCallback(AsyncReveiveMessage), socket);
                }
                catch (Exception)
                {
                    //设置计数器
                    //_count--;
                    //断开连接
                    Console.WriteLine(clientipe + " is disconnected，total connects " , ConsoleColor.Red);
                }
            }

        }

    }

 }
