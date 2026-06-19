using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MoonSharp.Interpreter;

namespace FlowEngine.Engine
{
    /// <summary>
    /// Represents a network socket wrapper (TCP Server/Client, UDP Server/Client)
    /// registered for MoonSharp scripts to perform timeout control and bytes transmission/reception.
    /// </summary>
    [MoonSharpUserData]
    public class LuaSocket : IDisposable
    {
        private enum SocketMode { TcpServer, TcpClient, UdpServer, UdpClient }
        private readonly SocketMode _mode;
        
        private TcpListener? _tcpListener;
        private TcpClient? _tcpClient;
        private UdpClient? _udpClient;
        private IPEndPoint? _udpRemoteEndPoint;
        
        private int _timeoutMs = -1; // -1 means infinite/blocking

        private LuaSocket(TcpListener listener)
        {
            _mode = SocketMode.TcpServer;
            _tcpListener = listener;
        }

        private LuaSocket(TcpClient client)
        {
            _mode = SocketMode.TcpClient;
            _tcpClient = client;
        }

        private LuaSocket(UdpClient client, SocketMode mode, IPEndPoint? remoteEP = null)
        {
            _mode = mode;
            _udpClient = client;
            _udpRemoteEndPoint = remoteEP;
        }

        /// <summary>
        /// Creates a TCP Server socket listening on the specified port.
        /// </summary>
        public static LuaSocket? CreateTcpServer(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                return new LuaSocket(listener);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create TCP Server: {ex.Message}");
                // throw;
                return null;
            }
        }

        /// <summary>
        /// Connects a TCP Client to the specified IP address and port.
        /// </summary>
        public static LuaSocket? ConnectTcpClient(string ip, int port)
        {
            try
            {
                var client = new TcpClient();
                client.Connect(ip, port);
                return new LuaSocket(client);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to connect TCP Client: {ex.Message}");
                // throw;
                return null;
            }
        }

        /// <summary>
        /// Creates a UDP Server bound to the specified port.
        /// </summary>
        public static LuaSocket? CreateUdpServer(int port)
        {
            try
            {
                var client = new UdpClient(port);
                return new LuaSocket(client, SocketMode.UdpServer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create UDP Server: {ex.Message}");
                // throw;
                return null;
            }
        }

        /// <summary>
        /// Creates a UDP Client connected to the target IP and port.
        /// </summary>
        public static LuaSocket? ConnectUdpClient(string ip, int port)
        {
            try
            {
                var client = new UdpClient();
                var remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
                client.Connect(remoteEP);
                return new LuaSocket(client, SocketMode.UdpClient, remoteEP);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to connect UDP Client: {ex.Message}");
                // throw;
                return null;
            }
        }

        /// <summary>
        /// Configures connection, send, and receive timeout in milliseconds.
        /// </summary>
        public void set_timeout(double timeoutMs)
        {
            _timeoutMs = (int)timeoutMs;
            
            if (_tcpClient != null)
            {
                _tcpClient.ReceiveTimeout = _timeoutMs;
                _tcpClient.SendTimeout = _timeoutMs;
            }
            if (_udpClient != null)
            {
                _udpClient.Client.ReceiveTimeout = _timeoutMs;
                _udpClient.Client.SendTimeout = _timeoutMs;
            }
        }

        /// <summary>
        /// Transmits a byte array package from a MoonSharp table.
        /// </summary>
        public void transmit(Table bytes)
        {
            if (bytes == null) return;
            
            var byteList = new List<byte>();
            foreach (var pair in bytes.Pairs)
            {
                if (pair.Value.Type == DataType.Number)
                {
                    byteList.Add((byte)pair.Value.Number);
                }
            }
            byte[] data = byteList.ToArray();
            if (data.Length == 0) return;

            try
            {
                if (_mode == SocketMode.TcpClient)
                {
                    if (_tcpClient != null && _tcpClient.Connected)
                    {
                        var stream = _tcpClient.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                }
                else if (_mode == SocketMode.TcpServer)
                {
                    EnsureTcpServerClient();
                    if (_tcpClient != null && _tcpClient.Connected)
                    {
                        var stream = _tcpClient.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                }
                else if (_mode == SocketMode.UdpClient)
                {
                    if (_udpClient != null)
                    {
                        _udpClient.Send(data, data.Length);
                    }
                }
                else if (_mode == SocketMode.UdpServer)
                {
                    if (_udpClient != null && _udpRemoteEndPoint != null)
                    {
                        _udpClient.Send(data, data.Length, _udpRemoteEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException($"Transmit failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Receives network packets and returns them as a MoonSharp byte table.
        /// </summary>
        public Table receive(ScriptExecutionContext context)
        {
            var table = new Table(context.OwnerScript);
            byte[]? readData = null;

            try
            {
                if (_mode == SocketMode.TcpClient)
                {
                    if (_tcpClient != null && _tcpClient.Connected)
                    {
                        var stream = _tcpClient.GetStream();
                        byte[] buffer = new byte[8192];
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            readData = new byte[read];
                            Buffer.BlockCopy(buffer, 0, readData, 0, read);
                        }
                    }
                }
                else if (_mode == SocketMode.TcpServer)
                {
                    EnsureTcpServerClient();
                    if (_tcpClient != null && _tcpClient.Connected)
                    {
                        var stream = _tcpClient.GetStream();
                        byte[] buffer = new byte[8192];
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            readData = new byte[read];
                            Buffer.BlockCopy(buffer, 0, readData, 0, read);
                        }
                    }
                }
                else if (_mode == SocketMode.UdpClient)
                {
                    if (_udpClient != null)
                    {
                        var ep = _udpRemoteEndPoint;
                        readData = _udpClient.Receive(ref ep);
                    }
                }
                else if (_mode == SocketMode.UdpServer)
                {
                    if (_udpClient != null)
                    {
                        var ep = new IPEndPoint(IPAddress.Any, 0);
                        readData = _udpClient.Receive(ref ep);
                        _udpRemoteEndPoint = ep;
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                return table;
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException($"Receive failed: {ex.Message}");
            }

            if (readData != null)
            {
                for (int i = 0; i < readData.Length; i++)
                {
                    table[i + 1] = (int)readData[i];
                }
            }

            return table;
        }

        private void EnsureTcpServerClient()
        {
            if (_mode != SocketMode.TcpServer || _tcpListener == null) return;
            
            if (_tcpClient != null && _tcpClient.Connected)
            {
                bool isStillConnected = !((_tcpClient.Client.Poll(1000, SelectMode.SelectRead) && (_tcpClient.Client.Available == 0)));
                if (isStillConnected) return;
                
                _tcpClient.Dispose();
                _tcpClient = null;
            }

            if (_timeoutMs > 0)
            {
                var acceptTask = _tcpListener.AcceptTcpClientAsync();
                if (Task.WhenAny(acceptTask, Task.Delay(_timeoutMs)).Result == acceptTask)
                {
                    _tcpClient = acceptTask.Result;
                    _tcpClient.ReceiveTimeout = _timeoutMs;
                    _tcpClient.SendTimeout = _timeoutMs;
                }
                else
                {
                    throw new TimeoutException("TCP Server accept connection timeout.");
                }
            }
            else
            {
                _tcpClient = _tcpListener.AcceptTcpClient();
            }
        }

        /// <summary>
        /// Checks if bytes are available to be read from the socket.
        /// </summary>
        public bool has_data()
        {
            if (_tcpClient != null)
            {
                return _tcpClient.Connected && _tcpClient.Available > 0;
            }
            if (_udpClient != null)
            {
                return _udpClient.Available > 0;
            }
            return false;
        }

        /// <summary>
        /// Checks if the socket is currently connected or active.
        /// </summary>
        public bool is_connected()
        {
            if (_mode == SocketMode.TcpClient || _mode == SocketMode.TcpServer)
            {
                return _tcpClient != null && _tcpClient.Connected;
            }
            return _udpClient != null;
        }

        /// <summary>
        /// Retrieves the remote endpoint or local endpoint address of the socket.
        /// </summary>
        public string get_address()
        {
            if (_tcpClient != null && _tcpClient.Client.RemoteEndPoint != null)
            {
                return _tcpClient.Client.RemoteEndPoint.ToString() ?? "";
            }
            if (_udpRemoteEndPoint != null)
            {
                return _udpRemoteEndPoint.ToString();
            }
            if (_tcpListener != null)
            {
                return _tcpListener.LocalEndpoint.ToString() ?? "";
            }
            if (_udpClient != null && _udpClient.Client.LocalEndPoint != null)
            {
                return _udpClient.Client.LocalEndPoint.ToString() ?? "";
            }
            return "";
        }

        /// <summary>
        /// Disposes all wrapped TCP and UDP resources.
        /// </summary>
        public void Dispose()
        {
            _tcpClient?.Dispose();
            _tcpListener?.Stop();
            _udpClient?.Dispose();
        }
    }
}

