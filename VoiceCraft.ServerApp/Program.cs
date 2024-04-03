var socket = new VoiceCraft.Network.Sockets.VoiceCraft();

try
{
    socket.OnDisconnected += Socket_OnDisconnected;
    Console.WriteLine("Connecting...");
    await socket.ConnectAsync("127.0.0.1", 9050, 0, VoiceCraft.Core.PositioningTypes.ServerSided, "v1.0.4");
    Console.ReadKey();
    Console.WriteLine("Connected");
}
catch(Exception ex)
{
    Console.WriteLine(ex);
}

void Socket_OnDisconnected(string? reason = null)
{
    Console.WriteLine(reason);
}