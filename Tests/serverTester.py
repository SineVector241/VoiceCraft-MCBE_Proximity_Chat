import requests
import random

url = 'http://127.0.0.1:9050/'
success = False
while(True):
    try:
        loginKey = str(input("Server Login Key: "))
        packet = {
            'Type': 0,
            'LoginKey': loginKey,
            'PlayerKey': 0,
            'PlayerId': '',
            'Gamertag': '',
            'Players': [{
                "PlayerId": "",
                "DimensionId": "",
                "Location": {"x": 0, "y": 0, "z": 0},
                "Rotation": 0,
                "CaveDensity": 0.0
            }],
            'Settings': {
                "ProximityDistance": 30,
                "ProximityToggle": True
            }
        }

        print("\nSending Request...")
        x = requests.post(url, json = packet)
        print(f"Server Response: {x.text}\n")
        if(x.status_code == 200):
            success = True
            break
    except KeyboardInterrupt:
        print("\nProgram Terminated.")
        break
        

while(True and success):
    try:
        packet = {
            'Type': 1,
            'LoginKey': loginKey,
            'PlayerKey': 0,
            'PlayerId': '',
            'Gamertag': '',
            'Players': [{
                "PlayerId": "",
                "DimensionId": "",
                "Location": {"x": 0, "y": 0, "z": 0},
                "Rotation": 0
            }],
            'Settings': {
                "ProximityDistance": 30,
                "ProximityToggle": True
            }
        }

        print("1: Bind Participant\n2: Bind Participant(Quick)\n3: Update Participant\n4: Update Settings\n5: Get Settings\n6: Remove Participant\n7: Exit Program")
        action = int(input("Action to take (integer): "))

        if(action == 1):
            packet['Type'] = 1
            plrKey = int(input("Player Key: "))
            plrId = str(input("Player Id: "))
            plrGamertag = str(input("Player Gamertag: "))

            packet['PlayerKey'] = plrKey
            packet['PlayerId'] = plrId
            packet['Gamertag'] = plrGamertag

        elif(action == 2):
            packet['Type'] = 1
            plrKey = int(input("Player Key: "))

            packet['PlayerKey'] = plrKey
            packet['PlayerId'] = random.choice(["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"])
            packet['Gamertag'] = f"Dummy{random.randint(1, 10)}"
            print(f"Generated Player: PlayerId - {packet['PlayerId']}, Gamertag - {packet['Gamertag']}")

        elif(action == 3):
            packet['Type'] = 2
            plrId = str(input("Player Id: "))
            plrDim = str(input("Player DimensionId: "))
            plrLocX = int(input("Player Location - X: "))
            plrLocY = int(input("Player Location - Y: "))
            plrLocZ = int(input("Player Location - Z: "))
            plrRot = int(input("Player Rotation: "))

            packet['Players'][0]['PlayerId'] = plrId
            packet['Players'][0]['DimensionId'] = plrDim
            packet['Players'][0]['Location']['x'] = plrLocX
            packet['Players'][0]['Location']['y'] = plrLocY
            packet['Players'][0]['Location']['z'] = plrLocZ
            packet['Players'][0]['Rotation'] = plrRot

        elif(action == 4):
            packet['Type'] = 3
            proxDist = int(input("Proximity Distance: "))
            proxToggleInput = str(input("Proximity Toggle: "))
            proxToggle = False

            if(proxToggleInput.lower() == "true"):
                proxToggle = True
            elif(proxToggleInput.lower() == "false"):
                proxToggle = False
            else:
                raise ValueError('Error. Expected boolean value!.')

            print(proxToggle)
            packet['Settings']['ProximityDistance'] = proxDist
            packet['Settings']['ProximityToggle'] = proxToggle

        elif(action == 5):
            packet['Type'] = 4

        elif(action == 6):
            packet['Type'] = 5
            plrId = str(input("Player Id: "))
            packet['PlayerId'] = plrId

        elif(action == 7):
            print("\nProgram Terminated.")
            break

        print("\nSending Request...")
        x = requests.post(url, json = packet)
        print(f"Server Response: {x.text}\n")

    
    except KeyboardInterrupt:
        print("\nProgram Terminated.")
        break
    except Exception as e:
        print(f"Error. {e}")

#Login = 0
#Bind = 1
#Update = 2
#UpdateSettings = 3
#GetSettings = 4
#RemoveParticipant = 5