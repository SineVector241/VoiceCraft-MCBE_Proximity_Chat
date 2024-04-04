var client = new VoiceCraft.Network.Sockets.VoiceCraft();

await client.ConnectAsync("127.0.0.1", 9050, 0, VoiceCraft.Core.PositioningTypes.ClientSided, "v1.0.4");
Console.ReadLine();