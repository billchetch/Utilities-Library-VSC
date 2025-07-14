using System;
using System.Net;
using System.Net.Sockets;

namespace Chetch.Utilities;


/// <summary>
/// As opposed to network sockets this is desiged for in machine
/// </summary>
public class LocalSocket
{

    #region Events
    public event EventHandler<byte[]> DataReceived;

    public event EventHandler<bool> Connected;
    #endregion

    #region Properties
    public bool IsConnected => socket != null && socket.Connected;
    #endregion

    #region Fields
    String path;
    public Socket socket;

    CancellationTokenSource lctSource;
    #endregion

    #region Constructors
    public LocalSocket(String path)
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
            throw new Exception(String.Format("Cannot connect this socket as it is already bound to {0}", path));
        }
        socket.Connect(new UnixDomainSocketEndPoint(path));
    }

    public void SendData(byte[] data)
    {
        socket.Send(data);    
    }

    public void StartListening()
    {
        if (lctSource == null)
        {
            lctSource = new CancellationTokenSource();
        }

        Listen(lctSource.Token);
    }

    public void Listen(CancellationToken ct)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        socket.Bind(new UnixDomainSocketEndPoint(path));
        socket.Listen();

        Task.Run(() =>
        {
            do
            {
                var newSocket = socket.Accept();
                if (!ct.IsCancellationRequested)
                {
                    byte[] buffer = new byte[256];
                    try
                    {
                        var n = newSocket.Receive(buffer);
                        if (DataReceived != null && n > 0)
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
            } while (!ct.IsCancellationRequested);
        }, ct);
    }

    public void StopListening()
    {
        lctSource.Cancel();
    }

    public void Disconnect()
    {
        //socket?.Shutdown(SocketShutdown.)
        if (IsConnected)
        {
            socket.Disconnect(false);
        }
    }

    public void Reconnect()
    {
        //TODO
    }
    #endregion
}
