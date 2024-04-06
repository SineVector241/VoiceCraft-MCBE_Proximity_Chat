var client = new VoiceCraft.Network.Sockets.VoiceCraft();

client.OnDisconnected += Client_OnDisconnected;

void Client_OnDisconnected(string? reason = null)
{
    Console.WriteLine(reason);
}

await client.ConnectAsync("127.0.0.1", 9050, 0, VoiceCraft.Core.PositioningTypes.ServerSided, "v1.0.4");
Console.ReadLine();