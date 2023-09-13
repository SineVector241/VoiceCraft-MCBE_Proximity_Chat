## Encapsulated Signalling Packets TCP
|PacketType|Data          |
|----------|--------------|
|Login     |LoginPacket   |
|Logout    |LogoutPacket  |
|Accept    |AcceptPacket  |
|Deny      |DenyPacket    |
|Binded    |BindedPacket  |
|Error     |ErrorPacket   |
|Ping      |PingPacket    |
|Deafen    |DeafenPacket  |
|Undeafen  |UndeafenPacket|
|Mute      |MutePacket    |
|Unmute    |UnmutePacket  |
|Null      |NODATA        |

Packet Length: 4 Bytes + DataLengthInBytes.

## Decapsulated Signalling Packets
### Login Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|PositioningType|int (4 Bytes)   |Define whether it is requesting ServerSided positioning or ClientSided positioning.|
|LoginKey       |ushort (2 Bytes)|Define the request key. If conflicted then choose the next available key and respond.|
|NameLength     |int (4 Bytes)   |Define the length of the name variable.|
|Name           |char[]          |Define the name of the participant. This is only used from server -> client.|

Permanent Length: 10 Bytes.

### Logout Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|LoginKey       |ushort (2 Bytes)|Define the participant to logout with the associated key. Only used from server -> client.|

Permanent Data Length: 2 Bytes.

### Accept Packet: ClientBound
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|LoginKey       |ushort (2 Bytes)|Define the key picked by the server.|
|VoicePort      |ushort (2 Bytes)|Define the voice port to connect to.|

Permanent Data Length: 4 bytes.

### Deny Packet: ClientBound
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|ReasonLength   |int (4 Bytes)   |Define the length of the reason variable.|
|Reason         |char[]          |Define the reason for the disconnection.|

Permanent Data Length: 4 bytes.

### Binded Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|NameLength     |int (4 Bytes)   |Define the length of the name variable.|
|Name           |char[]          |Define the name of the participant. This variable is used both ways.|

### Error Packet: ClientBound
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|ReasonLength   |int (4 Bytes)   |Define the length of the reason variable.|
|Reason         |char[]          |Define the reason for the error.|

### Ping Packet: Both
|Variable        |DataType        |Description|
|----------------|----------------|-----------|
|ServerDataLength|int (4 Bytes)   |Define the length of the server data variable.|
|ServerData      |char[]          |Define the data from the server. Only used from server -> client.|

### Deafen Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|LoginKey       |ushort (4 Bytes)|Define the participant to deafen. Only used from server -> client.|

### Undeafen Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|LoginKey       |ushort (4 Bytes)|Define the participant to undeafen. Only used from server -> client.|

### Mute Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|LoginKey       |ushort (4 Bytes)|Define the participant to mute. Only used from server -> client.|

### Unmute Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
|LoginKey       |ushort (4 Bytes)|Define the participant to unmute. Only used from server -> client.|

### Null Packet: Both
|Variable       |DataType        |Description|
|---------------|----------------|-----------|
