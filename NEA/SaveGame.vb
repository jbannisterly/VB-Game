Option Strict On
Imports NEA.CoordDataTypes
Imports System.Runtime.InteropServices
'// Class to manage the saving and loading of games
Public Class SaveGame
    Private Const SAVE_FOLDER As String = "Saved_Games"

    '// Save all game data to a file
    Public Shared Sub SaveGame(saveFile As String, ByRef character As Player, timeOfDay As Single, ByRef NPCs As NPCManager)
        Dim folderPath As String = SAVE_FOLDER & "\" & ValidFileString(saveFile)
        SaveWorldData(folderPath, character, timeOfDay)
        SaveInventory(folderPath, character)
        SaveMissions(folderPath, character)
        SaveEnemies(folderPath, character.missionList.enemies)
        SaveNPCs(folderPath, NPCs)
    End Sub

    '// Get save data for NPCs and write to file
    Private Shared Sub SaveNPCs(folderPath As String, ByRef NPCs As NPCManager)
        Dim saveData As Byte() = NPCs.GetNPCData()
        IO.File.WriteAllBytes(folderPath & "\NPC.txt", saveData)
    End Sub

    '// Get save data for enemies and write to file
    Private Shared Sub SaveEnemies(folderPath As String, ByRef enemies As EnemyManager)
        Dim saveData As Byte() = enemies.GetSaveData()
        IO.File.WriteAllBytes(folderPath & "\Enemies.txt", saveData)
    End Sub

    '// Generate mission progress save data and write to file
    Private Shared Sub SaveMissions(folderPath As String, ByRef character As Player)
        '// Size of the mission instance structure in bytes
        Const missionSize As Integer = 12

        Dim missionData As Missions.MissionInstance()
        Dim missionInstance As New Missions.MissionInstance()
        Dim missionPtr As IntPtr
        Dim missionSizeTotal As Integer
        Dim rawData As Byte()

        '// Get mission save data
        missionData = character.missionList.currentMissions.ToArray()
        missionSizeTotal = missionSize * missionData.Length
        '// Copy mission data to memory
        missionPtr = Marshal.AllocHGlobal(missionSizeTotal)
        For i = 0 To missionData.Length - 1
            Marshal.WriteInt32(missionPtr + i * missionSize + 0, missionData(i).missionID)
            Marshal.WriteInt32(missionPtr + i * missionSize + 4, missionData(i).currentTask)
            Marshal.WriteInt32(missionPtr + i * missionSize + 8, missionData(i).currentTaskProgress)
        Next
        ReDim rawData(missionSizeTotal - 1)

        '// Convert to array of bytes
        Marshal.Copy(missionPtr, rawData, 0, missionSizeTotal)
        Marshal.FreeHGlobal(missionPtr)

        '// Write byte array to file
        IO.File.WriteAllBytes(folderPath & "\Missions.txt", rawData)
    End Sub

    '// Generate inventory save data and write to memory
    Private Shared Sub SaveInventory(folderPath As String, ByRef character As Player)
        Dim inventoryContents As Inventory.InventoryItem()
        Dim inventoryInstance As New Inventory.InventoryItem()
        Dim inventoryPtr As IntPtr
        Dim inventorySize As Integer
        Dim inventorySizeTotal As Integer
        Dim rawData As Byte()

        '// Get save data
        inventoryContents = character.inventoryItems.inventoryContents.ToArray()
        '// Copy save data to memory
        inventorySize = Marshal.SizeOf(inventoryInstance)
        inventorySizeTotal = inventorySize * inventoryContents.Length
        inventoryPtr = Marshal.AllocHGlobal(inventorySizeTotal)
        For i = 0 To inventoryContents.Length - 1
            Marshal.StructureToPtr(inventoryContents(i), inventoryPtr + i * inventorySize, False)
        Next
        ReDim rawData(inventorySizeTotal - 1)
        '// Copy save data to byte array
        Marshal.Copy(inventoryPtr, rawData, 0, inventorySizeTotal)
        Marshal.FreeHGlobal(inventoryPtr)

        '// Write save data to file
        IO.File.WriteAllBytes(folderPath & "\Inventory.txt", rawData)
    End Sub

    '// Generate global data about the scene and save to file
    Private Shared Sub SaveWorldData(folderPath As String, character As Player, timeOfDay As Single)
        Dim globalData As New DataStore
        Dim globalDataSize As Integer = Marshal.SizeOf(globalData)
        Dim globalDataPtr As IntPtr = Marshal.AllocHGlobal(globalDataSize)
        Dim rawData(globalDataSize - 1) As Byte

        '// Populate data store structure with current game state
        globalData.lastSave = DateTimeOffset.UtcNow.UtcTicks
        globalData.playerPosition = character.location
        globalData.timeOfDay = timeOfDay

        '// Copy to byte array
        Marshal.StructureToPtr(globalData, globalDataPtr, False)
        Marshal.Copy(globalDataPtr, rawData, 0, globalDataSize)
        Marshal.FreeHGlobal(globalDataPtr)

        '// Write to file
        IO.File.WriteAllBytes(folderPath & "\WorldData.txt", rawData)
    End Sub

    '// Load game save from a file
    Public Shared Sub LoadGame(saveFile As String, ByRef character As Player, ByRef timeOfDay As Single, ByRef NPCs As NPCManager)
        Dim validSaveFile As String = ValidFileString(saveFile)
        Dim folderPath As String = SAVE_FOLDER & "\" & validSaveFile
        Dim globalData As DataStore = GetWorldData(validSaveFile)

        '// Reset player and NPCs
        InitialiseCharacter(character)
        NPCs.Initialise()

        '// Clear inventory and add inventory contents from file
        character.inventoryItems.inventoryContents.Clear()
        character.inventoryItems.inventoryContents.AddRange(GetInventoryData(folderPath))
        character.inventoryItems.RefreshInventory()

        '// Clear missions and add mission list from file
        character.missionList.currentMissions.Clear()
        character.missionList.currentMissions.AddRange(GetMissionData(folderPath))

        '// Load enemies an NPCs
        GetEnemyData(folderPath, character)
        GetNPCData(folderPath, NPCs)

        '// Set world states
        character.location = globalData.playerPosition
        timeOfDay = globalData.timeOfDay
    End Sub

    '// Load data from a file to get NPC progress
    Private Shared Sub GetNPCData(folderPath As String, ByRef NPCs As NPCManager)
        Dim rawData As Byte()
        rawData = IO.File.ReadAllBytes(folderPath & "\NPC.txt")
        NPCs.LoadNPCData(rawData)
    End Sub

    '// Get an array of all save game data
    Public Shared Function GetSavedGames() As LoadGameItem()
        Dim gameNames As String() = IO.Directory.GetDirectories(SAVE_FOLDER)
        Dim games(gameNames.Length - 1) As LoadGameItem

        '// Parse game name data
        For i = 0 To games.Length - 1
            games(i).worldName = gameNames(i).Split("\"c)(1)
            games(i).globalData = GetWorldData(games(i).worldName)
        Next

        Return games
    End Function

    '// Load mission progress from a file
    Private Shared Function GetMissionData(folderPath As String) As Missions.MissionInstance()
        Const missionSize As Integer = 12
        Dim missionDataPtr As IntPtr
        Dim missionData As Missions.MissionInstance()
        Dim rawData As Byte()

        '// Get byte data
        rawData = IO.File.ReadAllBytes(folderPath & "\Missions.txt")
        '// Copy to memory
        missionDataPtr = Marshal.AllocHGlobal(rawData.Length)
        Marshal.Copy(rawData, 0, missionDataPtr, rawData.Length)
        ReDim missionData(rawData.Length \ 12 - 1)
        '// Convert to mission instance structures
        For i = 0 To missionData.Length - 1
            missionData(i) = New Missions.MissionInstance()
            missionData(i).missionID = Marshal.ReadInt32(missionDataPtr + i * missionSize + 0)
            missionData(i).currentTask = Marshal.ReadInt32(missionDataPtr + i * missionSize + 4)
            missionData(i).currentTaskProgress = Marshal.ReadInt32(missionDataPtr + i * missionSize + 8)
        Next
        Marshal.FreeHGlobal(missionDataPtr)

        Return missionData
    End Function

    '// Load enemy data from a file
    Private Shared Sub GetEnemyData(folderPath As String, ByRef character As Player)
        Dim rawData As Byte()
        rawData = IO.File.ReadAllBytes(folderPath & "\Enemies.txt")
        character.missionList.enemies.LoadFromSaveData(rawData)
    End Sub

    '// Load inventory data from a file
    Private Shared Function GetInventoryData(folderPath As String) As Inventory.InventoryItem()
        Dim inventoryData As Inventory.InventoryItem
        Dim inventoryItemSize As Integer = Marshal.SizeOf(inventoryData)
        Dim inventoryDataPtr As IntPtr
        Dim inventoryItems As Inventory.InventoryItem()
        Dim rawData As Byte()

        '// Get raw data as an array of bytes
        rawData = IO.File.ReadAllBytes(folderPath & "\Inventory.txt")
        '// Copy to memory
        inventoryDataPtr = Marshal.AllocHGlobal(rawData.Length)
        Marshal.Copy(rawData, 0, inventoryDataPtr, rawData.Length)
        ReDim inventoryItems(rawData.Length \ inventoryItemSize - 1)
        '// Cast from memory to inventory structures
        For i = 0 To inventoryItems.Length - 1
            inventoryItems(i) = CType(Marshal.PtrToStructure(inventoryDataPtr + i * inventoryItemSize, inventoryData.GetType()), Inventory.InventoryItem)
        Next
        Marshal.FreeHGlobal(inventoryDataPtr)

        Return inventoryItems
    End Function

    '// Load global state data from a gile
    Private Shared Function GetWorldData(saveFile As String) As DataStore
        Dim globalData As New DataStore()
        Dim globalDataSize As Integer = Marshal.SizeOf(globalData)
        Dim globalDataPtr As IntPtr = Marshal.AllocHGlobal(globalDataSize)
        Dim rawData As Byte()
        Dim folderPath As String = SAVE_FOLDER & "\" & saveFile

        '// Read raw data to byte array
        rawData = IO.File.ReadAllBytes(folderPath & "\WorldData.txt")
        '// Cast to more useful structure
        Marshal.Copy(rawData, 0, globalDataPtr, globalDataSize)
        globalData = CType(Marshal.PtrToStructure(globalDataPtr, globalData.GetType()), DataStore)
        Marshal.FreeHGlobal(globalDataPtr)

        Return globalData
    End Function

    '// Check if the password enters matches the hash
    Public Shared Function CorrectPassword(saveName As String, password As String) As Boolean
        '// Hash password entered and load correct hash
        Dim hashedPassword As Byte() = HashPassword(password)
        Dim targetHash As Byte() = IO.File.ReadAllBytes(SAVE_FOLDER & "\" & saveName & "\Password.password")
        '// Compare each byte and reject if any are different
        For i = 0 To hashedPassword.Length - 1
            If hashedPassword(i) <> targetHash(i) Then Return False
        Next
        Return True
    End Function

    '// Reset player to default so files can be loaded
    Private Shared Sub InitialiseCharacter(ByRef character As Player)
        character.inventoryItems.inventoryContents.Clear()
        character.health = 100
        character.weapon.baseData = character.inventoryItems.itemEncyclopedia(0)
        character.shield.baseData = character.inventoryItems.itemEncyclopedia(0)
        character.inventoryItems.AddItem("No Item", 1, 1)
        character.missionList.currentMissions.Clear()
        character.location = New COORD3Sng(300, 0, 300)
        character.missionList.enemies.enemies.Clear()
        character.children(0).model = character.inventoryItems.itemEncyclopedia(0).model
        character.children(1).model = character.inventoryItems.itemEncyclopedia(0).model
    End Sub

    '// Create a new game and save to a file
    Public Shared Sub CreateGame(saveName As String, password As String, ByRef character As Player, timeOfDay As Single, ByRef NPCs As NPCManager)
        If Not IO.Directory.Exists(SAVE_FOLDER) Then IO.Directory.CreateDirectory(SAVE_FOLDER)
        '// Create save file
        saveName = ValidFileString(saveName)
        IO.Directory.CreateDirectory(SAVE_FOLDER & "\" & saveName)
        IO.File.WriteAllBytes(SAVE_FOLDER & "\" & saveName & "\Password.password", HashPassword(password))
        '// Initialise game
        InitialiseCharacter(character)
        NPCs.Initialise()
        '// Save state of new game
        SaveGame(saveName, character, timeOfDay, NPCs)
    End Sub

    '// Convert spaces to underscores so name is a valid file name
    Public Shared Function ValidFileString(name As String) As String
        Dim newName As New Text.StringBuilder()
        For i = 0 To name.Length - 1
            If name(i) = " "c Then
                newName.Append("_"c)
            Else
                newName.Append(name(i))
            End If
        Next
        Return newName.ToString()
    End Function

    '// Use SHA256 to hash a password
    Private Shared Function HashPassword(password As String) As Byte()
        Dim hasher As Security.Cryptography.SHA256 = Security.Cryptography.SHA256.Create()
        Dim passwordData(password.Length - 1) As Byte
        '// Convert password to byte array
        For i = 0 To password.Length - 1
            passwordData(i) = CByte(Asc(password(i)))
        Next
        '// Perform hash
        Return hasher.ComputeHash(passwordData)
    End Function

    '// Replace underscores with spaces when displaying the file name
    Public Shared Function FileStringDisplay(fileName As String) As String
        Dim newName As New Text.StringBuilder()
        For i = 0 To fileName.Length - 1
            If fileName(i) = "_"c Then
                newName.Append(" "c)
            Else
                newName.Append(fileName(i))
            End If
        Next
        Return newName.ToString()
    End Function

    '// Check if a world name already exists
    Public Shared Function Duplicate(saveName As String) As Boolean
        If IO.Directory.Exists(SAVE_FOLDER & "\" & ValidFileString(saveName)) Then
            Return True
        Else
            Return False
        End If
    End Function

    '// Check if a password meets all the requirements using regex
    Public Shared Function StrongPassword(password As String, ByRef errorMessage As String) As Boolean
        If Not Text.RegularExpressions.Regex.Match(password, ".*[A-Za-z].*").Success Then
            '// Check if password contains a letter
            errorMessage = "Password must have a letter"
            Return False
        ElseIf Not Text.RegularExpressions.Regex.Match(password, ".*\d.*").Success Then
            '// Check if password contains a digit
            errorMessage = "Password must have a number"
            Return False
        ElseIf Not Text.RegularExpressions.Regex.Match(password, ".{5}.*").Success Then
            '// Check if password has at least 5 characters
            errorMessage = "Password must be at least 5 characters"
            Return False
        Else
            '// Password meets all the above conditions, so is valid
            Return True
        End If
    End Function

    '// Read data from a file and return an array of 32-bit integers
    Public Shared Function ReadAllInt(fileName As String) As Integer()
        If IO.File.Exists(fileName) Then
            Return ToInt(IO.File.ReadAllBytes(fileName))
        End If
        Return {}
    End Function

    '// Write an array of 32-bit integers to a file
    Public Shared Sub WriteAllInt(fileName As String, data As Integer())
        IO.File.WriteAllBytes(fileName, ToByte(data))
    End Sub

    '// Cast 32-bit integer array to byte array
    Private Shared Function ToByte(data As Integer()) As Byte()
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length * 4)
        Dim byteData(data.Length * 4 - 1) As Byte
        Marshal.Copy(data, 0, dataPtr, data.Length)
        Marshal.Copy(dataPtr, byteData, 0, data.Length * 4)
        Marshal.FreeHGlobal(dataPtr)
        Return byteData
    End Function

    '// Cast byte array to 32-bit integer array
    Private Shared Function ToInt(data As Byte()) As Integer()
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length)
        Dim intData(data.Length \ 4 - 1) As Integer
        Marshal.Copy(data, 0, dataPtr, data.Length)
        Marshal.Copy(dataPtr, intData, 0, data.Length \ 4)
        Marshal.FreeHGlobal(dataPtr)
        Return intData
    End Function

    '// Structures

    <StructLayout(LayoutKind.Sequential)>
    Structure DataStore
        Public lastSave As Long
        Public playerPosition As COORD3Sng
        Public timeOfDay As Single
    End Structure
    Structure LoadGameItem
        Public worldName As String
        Public globalData As DataStore
    End Structure
End Class

