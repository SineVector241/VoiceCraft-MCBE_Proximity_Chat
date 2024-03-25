## Encapsulated Signalling Packets TCP
|PacketType|Data                                                                                                                                    |
|----------|----------------------------------------------------------------------------------------------------------------------------------------|
|Login     |[LoginPacket](#login-packet-both)         |
|Logout    |[LogoutPacket](#logout-packet-both)       |
|Accept    |[AcceptPacket](#accept-packet-clientbound)|
|Deny      |[DenyPacket](#deny-packet-clientbound)    |
|Binded    |[BindedPacket](#binded-packet-both)       |
|Unbinded  |[UnbindedPacket](#unbinded-packet-serverbound)  |
|Deafen    |[DeafenPacket](#deafen-packet-both)       |
|Undeafen  |[UndeafenPacket](#undeafen-packet-both)   |
|Mute      |[MutePacket](#mute-packet-both)           |
|Unmute    |[UnmutePacket](#unmute-packet-both)       |
|Error     |[ErrorPacket](#error-packet-clientbound)  |
|Ping      |[PingPacket](#ping-packet-both)           |
|Null      |[NullPacket](#null-packet-both)           |

Packet Length: 4 Bytes + DataLengthInBytes.

## Decapsulated Signalling Packets
### Login Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|PositioningType|int (2 Bytes)   |Define whether it is requesting ServerSided positioning or ClientSided positioning.|
|LoginKey       |ushort (2 Bytes)|Define the request key. If conflicted then choose the next available key and respond.|
|IsDeafened     |bool (1 Byte)   |Define if the participant is deafened. This is only used from server -> client|
|IsMuted        |bool (1 Byte)   |Define if the participant is muted. This is only used from server -> client|
|NameLength     |int (4 Bytes)   |Define the length of the name variable.|
|VersionLength  |int (4 Bytes)   |Define the length of the version variable.|
|Name           |char[]          |Define the name of the participant. This is only used from server -> client.|
|Version        |char[]          |Define the version of the client. This is only used from client -> server.|

Permanent Length: 14 Bytes.

### Logout Packet: Both
|Variable|DataType        |Description|
|--------|----------------|-----------|
|LoginKey|ushort (2 Bytes)|Define the participant to logout with the associated key. Only used from server -> client.|

Permanent Data Length: 2 Bytes.

### Accept Packet: ClientBound
|Variable |DataType        |Description|
|---------|----------------|-----------|
|LoginKey |ushort (2 Bytes)|Define the key picked by the server.|
|VoicePort|ushort (2 Bytes)|Define the voice port to connect to.|

Permanent Data Length: 4 bytes.

### Deny Packet: ClientBound
|Variable    |DataType     |Description|
|------------|-------------|-----------|
|ReasonLength|int (4 Bytes)|Define the length of the reason variable.|
|Reason      |char[]       |Define the reason for the deny.|

Permanent Data Length: 4 bytes.

### Binded Packet: Both
|Variable  |DataType     |Description|
|----------|-------------|-----------|
|NameLength|int (4 Bytes)|Define the length of the name variable.|
|Name      |char[]       |Define the name of the participant. This variable is used both ways.|

Permanent Data Length: 4 bytes.

### Unbinded Packet: ServerBound
|Variable  |DataType     |Description|
|----------|-------------|-----------|

Permanent Data Length: 0 bytes.

### Deafen Packet: Both
|Variable|DataType        |Description|
|--------|----------------|-----------|
|LoginKey|ushort (2 Bytes)|Define the participant to deafen. Only used from server -> client.|

Permanent Data Length: 2 bytes.

### Undeafen Packet: Both
|Variable|DataType        |Description|
|--------|----------------|-----------|
|LoginKey|ushort (2 Bytes)|Define the participant to undeafen. Only used from server -> client.|

Permanent Data Length: 2 bytes.

### Mute Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|LoginKey       |ushort (2 Bytes)|Define the participant to mute. Only used from server -> client.|

Permanent Data Length: 2 bytes.

### Unmute Packet: Both
|Variable|DataType        |Description|
|--------|----------------|-----------|
|LoginKey|ushort (2 Bytes)|Define the participant to unmute. Only used from server -> client.|

Permanent Data Length: 2 bytes.

### Error Packet: ClientBound
|Variable    |DataType     |Description|
|------------|-------------|-----------|
|ReasonLength|int (4 Bytes)|Define the length of the reason variable.|
|Reason      |char[]       |Define the reason for the error.|

Permanent Data Length: 4 bytes.

### Ping Packet: Both
|Variable        |DataType     |Description|
|----------------|-------------|-----------|
|ServerDataLength|int (4 Bytes)|Define the length of the server data variable.|
|ServerData      |char[]       |Define the data from the server. Only used from server -> client.|

Permanent Data Length: 4 bytes.

### Null Packet: Both
|Variable|DataType|Description|
|--------|--------|-----------|

Permanent Data Length: 0 bytes.

## Encapsulated Voice Packets UDP
|PacketType    |Data                                                                                                                                                 |
|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
|Login         |[LoginPacket](#login-packet-serverbound)               |
|Accept        |[AcceptPacket](#accept-packet-clientbound-1)           |
|Deny          |[DenyPacket](#deny-packet-clientbound-1)               |
|ClientAudio     |[SendAudioPacket](#client-audio-packet-serverbound)      |
|ServerAudio  |[ReceiveAudioPacket](#server-audio-packet-clientbound)|
|UpdatePosition|[UpdatePositionPacket](#update-position-packet-serverbound)   |
|Null          |[NullPacket](#null-packet-both-1)                      |

Packet Length: 2 Bytes + DataLengthInBytes.

## Decapsulated Voice Packets
### Login Packet: ServerBound
|Variable|DataType|Description|
|--------|----------------|-----------|
|LoginKey|ushort (2 Bytes)|Login key. This is just used so if there are multiple clients from the same IP they do not conflict|

Permanent Data Length: 2 bytes.

### Accept Packet: ClientBound
|Variable|DataType|Description|
|--------|--------|-----------|

Permanent Data Length: 0 bytes.

### Deny Packet: ClientBound
|Variable    |DataType     |Description|
|------------|-------------|-----------|
|ReasonLength|int (4 Bytes)|Define the length of the reason variable.|
|Reason      |char[]       |Define the reason for the deny.|

Permanent Data Length: 4 bytes.

### Client Audio Packet: ServerBound
|Variable   |DataType      |Description|
|-----------|--------------|-----------|
|PacketCount|uint (4 Bytes)|Define the packet count.|
|AudioLength|int (4 Bytes) |Define the length for the audio variable.|
|Audio      |byte[]        |Define the audio data to send to all other participants.|

Permanent Data Length: 8 bytes.

### Server Audio Packet: ClientBound
|Variable   |DataType        |Description|
|-----------|----------------|-----------|
|LoginKey   |ushort (2 Bytes)|Define the participant key the voice packet associates to.|
|PacketCount|uint (4 Bytes)  |Define the count of the voice packet.|
|Volume     |float (4 Bytes) |Define the volume to set the participant volume at.|
|EchoFactor |float (4 Bytes) |Define the echo to set the participants echo effect at.|
|Rotation   |float (4 Bytes) |Define the rotation effect for directional hearing.|
|Muffled    |bool (1 byte)   |Define if the audio should be muffled for underwater effect.|
|AudioLength|int (4 Bytes)   |Define the length for the audio variable.|
|Audio      |byte[]          |Define the audio data to input into the buffer for the participant.|

Permanent Data Length: 23 bytes.

### Update Position Packet: ServerBound
|Variable           |DataType        |Description|
|-------------------|---------------|-----------|
|x                  |float (4 Bytes)|Define X coordinate of the player.|
|y                  |float (4 Bytes)|Define Y coordinate of the player.|
|z                  |float (4 Bytes)|Define Z coordinate of the player.|
|EnvironmentIdLength|int (4 Bytes)  |Define length of the environment ID variable.|
|EnvironmentId      |char[]         |Define the environment the player is currently in.|

Permanent Data Length: 16 bytes.

### Null Packet: Both
|Variable|DataType|Description|
|--------|--------|-----------|

Permanent Data Length: 0 bytes.
