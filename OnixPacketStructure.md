## Encapsulated Packet Data

|PacketId|Data                               |
|--------|-----------------------------------|
|Login   |[LoginPacket](#login-serverbound)  |
|Logout  |[LogoutPacket](#logout-serverbound)|
|Accept  |[AcceptPacket](#accept-clientbound)|
|Deny    |[DenyPacket](#deny-clientbound)    |
|Update  |[UpdatePacket](#update-serverbound)|

Constant Packet Size: 1 byte.


## Login: ServerBound

|Variable  |Data Type    |Description                             |
|----------|-------------|----------------------------------------|
|NameLength|int (4 bytes)|Defines the length of the name variable.|
|Name      |char[]       |The name of the player                  |

Constant Packet Size: 4 bytes.

Constant Encapsulated Packet Size: 5 bytes.

## Logout: ServerBound

|Variable|Data Type|Description|
|--------|---------|-----------|

Constant Packet Size: 0 bytes.

Constant Encapsulated Packet Size: 1 byte.

## Accept: ClientBound

|Variable|Data Type|Description|
|--------|---------|-----------|

Constant Packet Size: 0 bytes.
Constant Encapsulated Packet Size: 1 byte.

## Deny: ClientBound
|Variable    |Data Type    |Description                               |
|------------|-------------|------------------------------------------|
|ReasonLength|int (4 bytes)|Defines the length of the reason variable.|
|Reason      |char[]       |The reason of the denial                  |

Constant Packet Size: 4 bytes.

Constant Encapsulated Packet Size: 5 bytes.

## Update: ServerBound
|Variable         |Data Type      |Description                                    |
|-----------------|---------------|-----------------------------------------------|
|X                |float (4 bytes)|Defines the player X coordinate.               |
|Y                |float (4 bytes)|Defines the player Z coordinate.               |
|Z                |float (4 bytes)|Defines the player Z coordinate.               |
|Rotation         |float (4 bytes)|Defines the player's head rotation in radians. |
|CaveDensity      |float (4 bytes)|Defines how surrounded the player is in a cave, This is basically used as an echo factor, Server calculation uses this like so... `Math.Max(player.CaveDensity, otherPlayer.Value.CaveDensity) * (1.0f - volume) : 0.0f;`|
|IsUnderwater     |bool (1 byte)  |Defines wether the player's head is underwater.|
|DimensionIdLength|int (4 bytes)  |Defines the length of the DimensionId variable.|
|LevelIdLength    |int (4 bytes)  |Defines the length of the LevelId variable.    |
|ServerIdLength   |int (4 bytes)  |Defines the length of the ServerId variable.   |
|DimensionId      |char[]         |The dimension the player is currently in.      |
|LevelId          |char[]         |The world Id the player is currently in.       |
|ServerId         |char[]         |The server the player is connected to. Can be IP and Port in string notation e.g. 127.0.0.1:19132|

Constant Packet Size: 33 bytes.

Constant Encapsulated Packet Size: 34 bytes.


## Protocol
Client > Server: Login Packet

Client < Server: Accept Packet

Server > VCServer: Bind Packet //You can ignore this.

(Looped)
Client > Server: Update Packet

Client < Server: Accept Packet

## Notes
Deny packet will only be used if the client has not sent an update packet within 8 seconds to which another login packet will need to be sent.

Logout packet is used to unbind the player from the VC server so they cannot hear any other players.
