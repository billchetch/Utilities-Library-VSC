using System;
using System.Net;
using System.Net.Sockets;

namespace Chetch.Utilities;


/// <summary>
/// As opposed to network sockets this is desiged for in machine
/// </summary>
public class LocalSocketConnection
{
    #region Events
    public event EventHandler<byte[]> DataReceived;

    public event EventHandler<bool> Connected;
    #endregion

    #region Properties
    public bool IsConnected => socket != null && socket.Connected;
    public bool IsBound => socket != null && socket.IsBound;

    public bool IsListening => listening;
    #endregion

    #region Fields
    String path;
    public Socket socket;
    public Socket sendSocket;

    CancellationTokenSource ctSource = new CancellationTokenSource();

    bool listening = false;
    #endregion

    #region Constructors
    public LocalSocketConnection(String path)
    {
        this.path = path;
        socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
    }
    #endregion

    #region Methods
    public void Connect()
    {
        if (socket.IsBound)
        {
            throw new NotImplementedException(String.Format("Cannot connect this socket as it is already bound to {0}", path));
        }
        socket.Connect(new UnixDomainSocketEndPoint(path));
        sendSocket = socket;
        Connected?.Invoke(this, true);

        Task.Run(() =>
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
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }, ctSource.Token);
    }

    public void SendData(byte[] data)
    {
        sendSocket.Send(data);    
    }

    public void StartListening()
    {
        if (IsConnected)
        {
            throw new NotImplementedException("Client socket cannot listen");
        }
        
        Listen(ctSource.Token);
    }

    public void Listen(CancellationToken ct)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        socket.Bind(new UnixDomainSocketEndPoint(path));
        socket.Listen();
        listening = true;

        Task.Run(() =>
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
        if (IsConnected)
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
        if (IsConnected)
        {
            ctSource?.Cancel();
            socket?.Shutdown(SocketShutdown.Send);
            socket.Disconnect(false);
            Connected?.Invoke(this, false);
        }
    }

    public void Reconnect()
    {
        //TODO
    }
    #endregion
}
