using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace SuperCom.Core.Telnet
{

    /// <summary>
    /// 参考：https://github.com/robertripoll/TelnetServer
    /// </summary>
    public class TelnetServer
    {
        /// <summary>
        /// Telnet's default port.
        /// </summary>
        private const int PORT = 23;
        /// <summary>
        /// End of line constant.
        /// </summary>
        public const string END_LINE = "\r\n";
        public const string CURSOR = " > ";

        /// <summary>
        /// Server's main socket.
        /// </summary>
        private Socket serverSocket;
        /// <summary>
        /// The IP on which to listen.
        /// </summary>
        public IPAddress ip { get; set; }
        /// <summary>
        /// The default data size for received data.
        /// </summary>
        private readonly int dataSize;
        /// <summary>
        /// Contains the received data.
        /// </summary>
        private byte[] data;
        /// <summary>
        /// True for allowing incoming connections;
        /// false otherwise.
        /// </summary>
        private bool acceptIncomingConnections;
        /// <summary>
        /// Contains all connected clients indexed
        /// by their socket.
        /// </summary>
        private Dictionary<Socket, TelnetClient> clients { get; set; }

        public delegate void ConnectionEventHandler(TelnetClient c);
        /// <summary>
        /// Occurs when a client is connected.
        /// </summary>
        public event ConnectionEventHandler ClientConnected;
        /// <summary>
        /// Occurs when a client is disconnected.
        /// </summary>
        public event ConnectionEventHandler ClientDisconnected;
        public delegate void ConnectionBlockedEventHandler(IPEndPoint endPoint);
        /// <summary>
        /// Occurs when an incoming connection is blocked.
        /// </summary>
        public event ConnectionBlockedEventHandler ConnectionBlocked;
        public delegate void MessageReceivedEventHandler(TelnetClient c, string message);
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        /// <param name="ip">The IP on which to listen to.</param>
        /// <param name="dataSize">Data size for received data.</param>
        public TelnetServer(IPAddress ip, int dataSize = 1024)
        {
            this.ip = ip;

            this.dataSize = dataSize;
            this.data = new byte[dataSize];

            this.clients = new Dictionary<Socket, TelnetClient>();

            this.acceptIncomingConnections = true;

            this.serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            serverSocket.Bind(new IPEndPoint(ip, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(new AsyncCallback(handleIncomingConnection), serverSocket);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            serverSocket.Close();
            kickAllClient();
        }

        /// <summary>
        /// Returns whether incoming connections
        /// are allowed.
        /// </summary>
        /// <returns>True is connections are allowed;
        /// false otherwise.</returns>
        public bool incomingConnectionsAllowed()
        {
            return acceptIncomingConnections;
        }

        /// <summary>
        /// Denies the incoming connections.
        /// </summary>
        public void denyIncomingConnections()
        {
            this.acceptIncomingConnections = false;
        }

        /// <summary>
        /// Allows the incoming connections.
        /// </summary>
        public void allowIncomingConnections()
        {
            this.acceptIncomingConnections = true;
        }

        /// <summary>
        /// Clears the screen for the specified
        /// client.
        /// </summary>
        /// <param name="c">The client on which
        /// to clear the screen.</param>
        public void clearClientScreen(TelnetClient c)
        {
            sendMessageToClient(c, "\u001B[1J\u001B[H");
        }

        /// <summary>
        /// Sends a text message to the specified
        /// client.
        /// </summary>
        /// <param name="c">The client.</param>
        /// <param name="message">The message.</param>
        public void sendMessageToClient(TelnetClient c, string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "";
            if (c != null && getSocketByClient(c) is Socket clientSocket)
                sendMessageToSocket(clientSocket, message);
        }

        /// <summary>
        /// Sends a text message to the specified
        /// socket.
        /// </summary>
        /// <param name="s">The socket.</param>
        /// <param name="message">The message.</param>
        private void sendMessageToSocket(Socket s, string message)
        {
            if (string.IsNullOrEmpty(message))
                message = "";
            try {
                byte[] data = Encoding.ASCII.GetBytes(message);
                sendBytesToSocket(s, data);
            } catch (Exception e) {
                Console.WriteLine(e);
            }

        }

        /// <summary>
        /// Sends bytes to the specified socket.
        /// </summary>
        /// <param name="s">The socket.</param>
        /// <param name="data">The bytes.</param>
        private void sendBytesToSocket(Socket s, byte[] data)
        {
            if (s == null || data == null)
                return;
            s.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendData), s);
        }

        /// <summary>
        /// Sends a message to all connected clients.
        /// </summary>
        /// <param name="message">The message.</param>
        public void sendMessageToAll(string message)
        {
            foreach (Socket s in clients.Keys) {
                try {
                    TelnetClient c = clients[s];
                    if (c.getCurrentStatus() == EClientStatus.LoggedIn) {
                        sendMessageToSocket(s, END_LINE + message + END_LINE + CURSOR);
                        c.resetReceivedData();
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                    clients.Remove(s);
                }
            }
        }

        /// <summary>
        /// Gets the client by socket.
        /// </summary>
        /// <param name="clientSocket">The client's socket.</param>
        /// <returns>If the socket is found, the client instance
        /// is returned; otherwise null is returned.</returns>
        private TelnetClient getClientBySocket(Socket clientSocket)
        {
            TelnetClient client;

            if (!clients.TryGetValue(clientSocket, out client))
                client = null;

            return client;
        }

        /// <summary>
        /// Gets the socket by client.
        /// </summary>
        /// <param name="client">The client instance.</param>
        /// <returns>If the client is found, the socket is
        /// returned; otherwise null is returned.</returns>
        private Socket getSocketByClient(TelnetClient client)
        {
            Socket socket = clients.FirstOrDefault(x => x.Value.getClientID() == client.getClientID()).Key;
            return socket;
        }

        /// <summary>
        /// Kicks the specified client from the server.
        /// </summary>
        /// <param name="client">The client.</param>
        public void kickClient(TelnetClient client)
        {
            ClientDisconnected(client);
            closeSocket(getSocketByClient(client));
        }

        /// <summary>
        /// Kicks the specified client from the server.
        /// </summary>
        /// <param name="client">The client.</param>
        public void kickAllClient()
        {
            if (clients == null || clients.Count == 0)
                return;
            List<TelnetClient> telnetClients = clients.Select(arg => arg.Value).ToList();
            foreach (TelnetClient client in telnetClients) {
                kickClient(client);
            }
        }

        /// <summary>
        /// Closes the socket and removes the client from
        /// the clients list.
        /// </summary>
        /// <param name="clientSocket">The client socket.</param>
        private void closeSocket(Socket clientSocket)
        {
            if (clientSocket == null)
                return;
            try {
                clientSocket.Close();
            } catch (Exception e) {
                Console.WriteLine(e);
            } finally {
                clients.Remove(clientSocket);
            }
        }

        /// <summary>
        /// Handles an incoming connection.
        /// If incoming connections are allowed,
        /// the client is added to the clients list
        /// and triggers the client connected event.
        /// Else, the connection blocked event is
        /// triggered.
        /// </summary>
        private void handleIncomingConnection(IAsyncResult result)
        {
            try {
                Socket oldSocket = (Socket)result.AsyncState;

                if (acceptIncomingConnections) {
                    Socket newSocket = oldSocket.EndAccept(result);

                    uint clientID = (uint)clients.Count + 1;
                    TelnetClient client = new TelnetClient(clientID, (IPEndPoint)newSocket.RemoteEndPoint);
                    clients.Add(newSocket, client);

                    sendBytesToSocket(
                        newSocket,
                        new byte[] {
                            0xff, 0xfd, 0x01,   // Do Echo
                            0xff, 0xfd, 0x21,   // Do Remote Flow Control
                            0xff, 0xfb, 0x01,   // Will Echo
                            0xff, 0xfb, 0x03    // Will Supress Go Ahead
                        }
                    );

                    client.resetReceivedData();

                    ClientConnected(client);

                    serverSocket.BeginAccept(new AsyncCallback(handleIncomingConnection), serverSocket);
                } else {
                    ConnectionBlocked((IPEndPoint)oldSocket.RemoteEndPoint);
                }
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Sends data to a socket.
        /// </summary>
        private void sendData(IAsyncResult result)
        {
            try {
                Socket clientSocket = (Socket)result.AsyncState;

                clientSocket.EndSend(result);

                clientSocket.BeginReceive(data, 0, dataSize, SocketFlags.None, new AsyncCallback(receiveData), clientSocket);
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Receives and processes data from a socket.
        /// It triggers the message received event in
        /// case the client pressed the return key.
        /// </summary>
        private void receiveData(IAsyncResult result)
        {
            try {
                Socket clientSocket = (Socket)result.AsyncState;
                TelnetClient client = getClientBySocket(clientSocket);

                int bytesReceived = clientSocket.EndReceive(result);

                if (bytesReceived == 0) {
                    closeSocket(clientSocket);
                    serverSocket.BeginAccept(new AsyncCallback(handleIncomingConnection), serverSocket);
                } else if (data[0] < 0xF0) {
                    string receivedData = client.getReceivedData();

                    // 0x2E = '.', 0x0D = carriage return, 0x0A = new line
                    if ((data[0] == 0x2E && data[1] == 0x0D && receivedData.Length == 0) ||
                        (data[0] == 0x0D && data[1] == 0x0A)) {
                        //sendMessageToSocket(clientSocket, "\u001B[1J\u001B[H");
                        MessageReceived(client, client.getReceivedData());
                        client.resetReceivedData();
                    } else {
                        // 0x08 => backspace character
                        if (data[0] == 0x08) {
                            if (receivedData.Length > 0) {
                                client.removeLastCharacterReceived();
                                sendBytesToSocket(clientSocket, new byte[] { 0x08, 0x20, 0x08 });
                            } else
                                clientSocket.BeginReceive(data, 0, dataSize, SocketFlags.None, new AsyncCallback(receiveData), clientSocket);
                        }

                        // 0x7F => delete character
                        else if (data[0] == 0x7F)
                            clientSocket.BeginReceive(data, 0, dataSize, SocketFlags.None, new AsyncCallback(receiveData), clientSocket);

                        else {
                            client.appendReceivedData(Encoding.ASCII.GetString(data, 0, bytesReceived));

                            // Echo back the received character
                            // if client is not writing any password
                            if (client.getCurrentStatus() != EClientStatus.Authenticating)
                                sendBytesToSocket(clientSocket, new byte[] { data[0] });

                            // Echo back asterisks if client is
                            // writing a password
                            else
                                sendMessageToSocket(clientSocket, "*");

                            clientSocket.BeginReceive(data, 0, dataSize, SocketFlags.None, new AsyncCallback(receiveData), clientSocket);
                        }
                    }
                } else
                    clientSocket.BeginReceive(data, 0, dataSize, SocketFlags.None, new AsyncCallback(receiveData), clientSocket);
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}
