# VoiceCraftProximityChat

Proximity Voice chat for Minecraft Bedrock Edition.

## Project Description
VoiceCraft proximity chat is a VOIP program developed on .NET 6.0 WPF framework developed in C# started by SineVector241 that enables Voice Proximity chat for the game Minecraft on the bedrock platform which gives a more immersive, communicative experience. This project will also expand into supporting more platforms such as IOS and Android devices using Xamarin. The project only allows for a self-hosted framework giving the owners more control on how they want it to be setup however the project may also expand into allowing multiple server instances on a single server that does not require a self-hosted framework and allows other users to host for other Minecraft server owners.

## Installation Guide
### Server Installation
1. Download the latest version of the Addon and VoiceCraft_Server.zip from the Releases page.
2. Install the addon onto a world with Beta API's enabled and put it onto a Bedrock Dedicated Server with "@minecraft/server-net" enabled in config/default/permissions.json
3. Extract and run VoiceCraft_Server.exe.
4. Connect to the minecraft server via minecraft and use the Key/LoginId given in the VoiceCraft server to type the following command `!vclink <IP> <Port> <Key>` (IP and Port is the address for the voicecraft server)
5. Congratulations. Your Minecraft Server and VoiceCraft server are now linked and your members can now start using voice proximity chat in your server!

## TODO LIST
- [x] Server to Server Commmunication
- [x] Client to Client VOIP
- [x] Server to Client verification
- [x] Server to Server Linking/Verification
- [x] Addon -- https://github.com/SineVector241/VoiceCraft-MCBE_ProximityChat_Addon
- [x] Rewrite/Improve Client side software
