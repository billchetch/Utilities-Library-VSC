using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Chetch.Utilities;


/// <summary>
/// As opposed to network sockets this is desiged for in machine use.
/// 
/// IMPORTANT: AddressNotAvailable (49) error!!!
/// In order for a client socket to connect there needs to be a socket file.
/// In order to create the socket file the Bind method should be called. This is done in the Listen method
/// so it's better to start by making the listening socket first and testing that which will create the file then after that
/// clients will not give an AddressNotAvailable error.
/// </summary>
public class LocalSocketConnection
{
    #region Constants
    public const int CONNECT_TIMER_INTERVAL = 2000;
    #endregion

    #region Events
    public event EventHandler<byte[]> DataReceived;

    public event EventHandler<bool> Connected;
    #endregion

    #region Properties
    public bool AutoConnect { get; set; } = true;
    public bool IsConnected => sendSocket != null && sendSocket.Connected;
    public bool IsBound => socket != null && socket.IsBound;
    public bool IsListening => listening;
    #endregion

    #region Fields
    String path;
    public Socket socket;
    public Socket sendSocket;

    CancellationTokenSource ctSource = new CancellationTokenSource();

    Object connectionLock = new Object();

    Task? connectionTask;
    Task? listeningTask;

    System.Timers.Timer? connectTimer = null;

    bool listening = false;
    #endregion

    #region Constructors
    public LocalSocketConnection(String path)
    {
        this.path = path;
    }
    #endregion

    #region Methods

    private void connect()
    {
        if (socket == null)
        {
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
        }
        
         //Here we connect
        socket.Connect(new UnixDomainSocketEndPoint(path));
        sendSocket = socket;
        Connected?.Invoke(this, true);

        //Fire up teh data received task
        connectionTask = Task.Run(() =>
        {
            byte[] buffer = new byte[256];
            while (!ctSource.IsCancellationRequested)
            {
                try
                {
                    int n = socket.Receive(buffer);
                    if (n > 0)
                    {
                        var data = buffer.Take(n).ToArray();
                        DataReceived?.Invoke(this, data);
                    }
                    else
                    {
                        Reconnect();
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }, ctSource.Token);
    }

    public void Connect()
    {
        if (socket != null && socket.IsBound)
        {
            throw new NotImplementedException(String.Format("Cannot connect this socket as it is already bound to {0}", path));
        }

        if (connectTimer == null)
        {
            connectTimer = new System.Timers.Timer();
            connectTimer.AutoReset = false;
            connectTimer.Interval = CONNECT_TIMER_INTERVAL;
            connectTimer.Elapsed += (sender, eargs) =>
            {
                connectTimer.Stop();

                lock (connectionLock)
                {
                    try
                    {
                        if (AutoConnect && !IsConnected)
                        {
                            connect();
                        }
                    }
                    catch (Exception e)
                    {
                        //TODO
                    }
                }
                connectTimer.Start();
            };
        }

        try
        {
            connect();
        }
        catch (System.Net.Sockets.SocketException)
        {
            //We leave so that the connect timer picks it up
            
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            connectTimer.Start();
        }
    }

    public void SendData(byte[] data)
    {
        sendSocket.Send(data);    
    }

    public void StartListening()
    {
        if (IsListening)
        {
            throw new Exception("Already listening");
        }
        
        Listen(ctSource.Token);
    }

    public void Listen(CancellationToken ct)
    {
        if (socket == null)
        {
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
        }

        if (socket == sendSocket)
        {
            throw new NotImplementedException("Cannot listen as already socket is in client mode");
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }
        //The binding call creates the socket file
        socket.Bind(new UnixDomainSocketEndPoint(path));
        socket.Listen();
        listening = true;

        listeningTask =Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    sendSocket = socket.Accept();
                    Connected.Invoke(this, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                while (!ct.IsCancellationRequested)
                {
                    byte[] buffer = new byte[256];
                    try
                    {
                        var n = sendSocket.Receive(buffer);
                        if (DataReceived != null && n > 0)
                        {
                            var data = buffer.Take(n).ToArray();
                            DataReceived?.Invoke(this, data);
                        }
                        else if (n == 0)
                        {
                            Connected.Invoke(this, false);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            listening = false;
        }, ct);
    }

    public void StopListening()
    {
        if (socket == sendSocket)
        {
            throw new NotImplementedException("Use Disconnect to disconnect client sockets");
        }
        ctSource?.Cancel();
        if (socket.Connected)
        {
            socket.Disconnect(false);    
        }
        if (sendSocket != null && sendSocket != socket && sendSocket.Connected)
        {
            sendSocket.Disconnect(false);
        }
    }

    public void Disconnect()
    {
        if (IsListening)
        {
            throw new NotImplementedException("Use StopListening to stop server sockets");
        }

        connectTimer?.Stop();
        if (IsConnected)
        {
            lock (connectionLock)
            {
                ctSource?.Cancel();
                socket?.Disconnect(true);
                sendSocket = null;
                Connected?.Invoke(this, false);
                socket?.Dispose();
                socket = null;
            }
        }
    }

    public async void Reconnect()
    {
        Disconnect();
        if (connectionTask != null)
        {
            try
            {
                await connectionTask;
            }
            catch (Exception)
            { }

            ctSource = new CancellationTokenSource();
        }
        connectTimer?.Start();
    }
    #endregion
}
