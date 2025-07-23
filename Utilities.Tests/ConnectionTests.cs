using Chetch.Utilities;
using Utilities;

namespace Utilities.Tests;

[TestClass]
public sealed class ConnectionTests
{
    [TestMethod]
    public void LocalSocketConnection()
    {
        var path = "/tmp/unix_socket_test";
        var cnn = new LocalSocketConnection(path);
        cnn.Connected += (sender, connected) =>
        {
            Console.WriteLine("Local sccket connected: {0}", connected);
        };
        try
        {
            cnn.Connect();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        DateTime started = DateTime.Now;
        while ((DateTime.Now - started).TotalSeconds < 40)
        {
            Thread.Sleep(1000);
        }

        cnn.Disconnect();
        Thread.Sleep(1000);
    }
}
