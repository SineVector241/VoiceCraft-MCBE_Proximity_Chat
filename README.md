# VoiceCraftProximityChat

Proximity Voice chat for Minecraft Bedrock Edition.

## Project Description
VoiceCraft proximity chat is a VOIP program developed on .NET 6.0 WPF framework developed in C# started by SineVector241 that enables Voice Proximity chat for the game Minecraft on the bedrock platform which gives a more immersive, communicative experience. This project will also expand into supporting more platforms such as IOS and Android devices using Xamarin. The project only allows for a self-hosted framework giving the owners more control on how they want it to be setup however the project may also expand into allowing multiple server instances on a single server that does not require a self-hosted framework and allows other users to host for other Minecraft server owners.

## Installation Guide
### Server Installation
1. Download the latest version of the Addon and VoiceCraft-Server.zip from the Releases page.
2. Install the addon onto a world with Beta API's enabled and put it onto a Bedrock Dedicated Server with "@minecraft/server-net" enabled in config/default/permissions.json
3. Extract and run VoiceCraft_Server.exe.
4. Connect to the minecraft server via minecraft and use the Key/LoginId given in the VoiceCraft server to type the following command `!connect <IP> <Port> <Key>` (IP and Port is the address for the voicecraft server)
5. Congratulations. Your Minecraft Server and VoiceCraft server are now linked and your members can now start using voice proximity chat in your server!

### PC Client Installation
1. Download the latest version of the VoiceCraft-PC.zip from the Releases page.
2. Run the VoiceCraftProximityChat.exe file. If you do not have the necessary drivers installed it will prompt you to install them with a link to the website.
3. Refer to using the app instructions further down.

### Android Client Installation
1. Download the latest version of com.sine.voicecraft_android.apk file from the Releases page.
2. Install the APK file by opening it and pressing install. Google may warn you about it so just dismiss if you see any pop up.
3. Your done. You can now open the app from the home screen. Refer to using the app instructions further down.

### Using the app.
1. Add a server by clicking...
- **Add Server(PC)**
- **+(Android)** <br><br> and filling in the required parameters for the VoiceCraft server you want to connect to provided by the server hoster.

2. Connect to the server by...
- **Selecting a Server then clicking Connect(PC)**
- **Clicking connect(Android)**
3. If you have successfully connected you will receive a key at the top of the app. If not then either the hoster has not setup the server correctly or your app's version does not match the server version.
4. Connect to the minecraft server the VoiceCraft server is linked to provided by the hoster through minecraft.
5. Use the key that was given to you earlier by the app to type the following command in chat `!bind <Key>`
6. If binding is successfull you should now be able to chat with other people.

## TODO LIST
- [x] Add AGC (Automatic Gain Control) for PC.
- [ ] Implement auto binding.
- [ ] Implement auto connecting/server linking.
- [ ] Implement experimental accurate player position depending on velocity and latency to client.
- [ ] Implement experimental directional hearing.
- [ ] Implement second experimental algorithm using Minecraft Websocket Client to Client Communication. (Allows voice proximity chat on any server but limits device support)
- [ ] Design a logo.
- [ ] Improve UI style/design.
