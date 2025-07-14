using System;
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
    Socket socket;
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
        socket.Connect(new UnixDomainSocketEndPoint(path));
    }

    public void SendData(byte[] data)
    {
        socket.Send(data);    
    } 

    public void Disconnect()
    {
        socket.Disconnect(false);
    }

    public void Reconnect()
    {
        //TODO
    }
    #endregion
}
