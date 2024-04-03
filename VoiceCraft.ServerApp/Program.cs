var socket = new VoiceCraft.Network.Sockets.VoiceCraft();

try
{
    socket.OnFailed += Socket_OnFailed;
    Console.WriteLine("Connecting...");
    await socket.HostAsync(9050);
    Console.ReadKey();
    Console.WriteLine("Connected");
}
catch(Exception ex)
{
    Console.WriteLine(ex);
}

void Socket_OnFailed(Exception error)
{
    Console.WriteLine(error);
}