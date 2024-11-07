Option Strict On
Imports System.Runtime.InteropServices
'// Class to represent a single interactable mob
Public Class NPC
    Inherits Mob
    '// Characters per second for dialogue
    Public Const TEXT_SPEED As Single = 20
    Private name As String
    Private commands As String()
    Private enemies As EnemyManager
    Private character As Player
    Private dialoguePoint As Integer
    Private dialoguePointRespawn As Integer

    '// Initialise NPC and set references
    Sub New(ByRef inModel As GLTFModel, ByRef inAudio As AudioManager, ByRef inCommands As String(), ByRef inName As String, ByRef inEnemies As EnemyManager, inCharacter As Player)
        MyBase.New(inModel, inAudio)
        ReDim commands(inCommands.Length - 2)
        Array.Copy(inCommands, 1, commands, 0, commands.Length)
        name = inName
        enemies = inEnemies
        character = inCharacter
    End Sub

    '// Reset dialogue progress
    Public Sub Initialise()
        dialoguePoint = 0
        dialoguePointRespawn = 0
    End Sub

    '// Load progress from file
    Public Sub LoadFromSaveData(rawData As Byte(), offset As Integer)
        Dim saveDataPtr As IntPtr = Marshal.AllocHGlobal(8)
        '// Cast byte to integer
        Marshal.Copy(rawData, offset, saveDataPtr, 8)
        dialoguePoint = Marshal.ReadInt32(saveDataPtr)
        dialoguePointRespawn = Marshal.ReadInt32(saveDataPtr + 4)
        '// Free memory
        Marshal.FreeHGlobal(saveDataPtr)
    End Sub

    '// Convert current state to array of bytes to be saved in a file
    Public Function GetSaveData() As Byte()
        Dim saveData(7) As Byte
        Dim saveDataPtr As IntPtr = Marshal.AllocHGlobal(saveData.Length)
        '// Write integers to memory and cast to bytes
        Marshal.WriteInt32(saveDataPtr, dialoguePoint)
        Marshal.WriteInt32(saveDataPtr + 4, dialoguePointRespawn)
        Marshal.Copy(saveDataPtr, saveData, 0, saveData.Length)
        '// Free memory
        Marshal.FreeHGlobal(saveDataPtr)
        Return saveData
    End Function

    '// Reset dialogue point
    Public Sub Respawn()
        dialoguePoint = dialoguePointRespawn
    End Sub

    '// Advance animation and look at character
    Public Sub Update(deltaT As Single)
        animationProgress += deltaT * model.animations.GetAnimationFrame(animationName).speed
        actualRotation = CSng(Math.Atan2(character.location.x - location.x, character.location.z - location.z))
    End Sub

    '// Move to next point in dialogue
    '// Returns true if end is reached
    Public Function AdvanceDialogue() As Boolean
        dialoguePoint += 1
        '// Check if dialogue is finished
        If dialoguePoint >= commands.Length Then
            Return True
        End If
        '// Advance thorugh and complete all commands
        While IsCommand(commands(dialoguePoint))
            PerformCommand(commands(dialoguePoint), dialoguePoint)
            If commands(dialoguePoint) = "!END" Then
                '// DIalogue finishes when !END command is reached
                Return True
            End If
            dialoguePoint += 1
        End While
        '// Check if dialogue finished by command being executed
        If dialoguePoint >= commands.Length Then
            Return True
        End If
        Return False
    End Function

    '// Highlight blue if the NPC is closest
    Protected Overrides Function GetHighlightColour() As CoordDataTypes.COORD3Sng
        If closest Then
            Return New CoordDataTypes.COORD3Sng(0, 0, 1)
        Else
            Return New CoordDataTypes.COORD3Sng(0, 0, 0)
        End If
    End Function

    '// Perform an action
    Private Sub PerformCommand(command As String, ByRef dialoguePoint As Integer)
        Dim splitCommand As String()
        Dim commandWord As String
        '// Extract command word as first word
        splitCommand = command.Split(" "c)
        commandWord = splitCommand(0).Substring(1).ToUpper()
        '// Perform action based on command word
        Select Case commandWord
            Case "SPAWN"
                SpawnMob(splitCommand)
            Case "MISSION"
                SetMission(splitCommand)
            Case "END"

            Case "GOTO"
                DialogueJump(splitCommand, dialoguePoint, commands)
            Case "GOTOIF"
                ConditionalDialogueJump(splitCommand, dialoguePoint, commands)
            Case "NEWTASK"
                AdvanceMission(splitCommand)
            Case "GIVE"
                GiveItem(splitCommand)
            Case "WEATHER"
                ControlWeather(splitCommand)
            Case "TAKE"
                TakeItem(splitCommand)
            Case "CHECKPOINT"
                dialoguePointRespawn = dialoguePoint
        End Select
    End Sub

    '// Change the weather by setting a boolean indicator
    Private Sub ControlWeather(splitCommand As String())
        Select Case splitCommand(1)
            Case "rain"
                GameWorld.isRaining = True
            Case "sun"
                GameWorld.isRaining = False
        End Select
    End Sub

    '// Add item to character inventory
    Private Sub GiveItem(splitCommand As String())
        character.inventoryItems.AddItem(splitCommand(1), CInt(splitCommand(2)), CInt(splitCommand(3)))
    End Sub

    '// Remove item from character inventory
    Private Sub TakeItem(splitCommand As String())
        Dim takeTarget As String = splitCommand(1)
        Dim takeQuantity As Integer = CInt(splitCommand(2))
        character.inventoryItems.RemoveItem(takeTarget, takeQuantity)
    End Sub

    '// Allows the NPC to have different responses based on task completion
    Private Sub ConditionalDialogueJump(splitCommand As String(), ByRef dialoguePoint As Integer, ByRef commands As String())
        '// Get task the NPC is checking
        Dim mission As String = splitCommand(2)
        Dim task As Integer = CInt(splitCommand(3))
        Dim missionIndex As Integer = character.missionList.GetMissionIndex(mission)
        If missionIndex > -1 AndAlso character.missionList.TaskComplete(missionIndex) Then
            '// Change the current line of dialogue if the task is complete
            DialogueJump(splitCommand, dialoguePoint, commands)
        End If
    End Sub

    '// Unconditional dialogue jump
    Private Sub DialogueJump(splitCommand As String(), ByRef dialoguePoint As Integer, ByRef commands As String())
        Dim target As String = splitCommand(1)
        Dim targetFound As Boolean = False
        dialoguePoint = -1
        '// Linear search for dialogue point with the corresponding label
        While Not targetFound
            dialoguePoint += 1
            If commands(dialoguePoint) = "!LABEL " & target Then
                targetFound = True
            End If
        End While
    End Sub

    '// Create a new mission
    Private Sub SetMission(splitCommand As String())
        character.missionList.SetMission(CInt(splitCommand(1)))
    End Sub

    '// Set the next task
    Private Sub AdvanceMission(splitCommand As String())
        character.missionList.AdvanceMission(splitCommand(1))
    End Sub

    '// Create a new enemy to fight when a challenge is set
    Private Sub SpawnMob(splitCommand As String())
        Dim mobLocation As New CoordDataTypes.COORD3Sng(CSng(splitCommand(2)), CSng(splitCommand(3)), CSng(splitCommand(4)))
        Dim quantity As Integer = 1
        '// There is an optional quantity parameter which defaults to 1
        If splitCommand.Length > 5 Then
            quantity = CInt(splitCommand(5))
        End If
        '// Spawn enemies
        For i = 1 To quantity
            enemies.SpawnEnemy(splitCommand(1), mobLocation)
            '// Ensure they do not spawn in the same place
            mobLocation.x += 0.2F
        Next
    End Sub

    '// Returns the line of dialogue the character is saying, or the last line if it has reached the end
    Public Function GetFullDialogue() As String
        If dialoguePoint >= commands.Length Then
            Return commands(commands.Length - 1)
        Else
            Return commands(dialoguePoint)
        End If
    End Function

    '// Returns part of the current dialogue string for typing effect
    Public Function GetDialogue(talkTime As Single) As String
        Dim dialogue As String
        Dim charactersToShow As Integer
        dialogue = GetFullDialogue()
        charactersToShow = CInt(talkTime * TEXT_SPEED)
        '// Ensure the number of characters does not exceed length of dialogue
        If charactersToShow > dialogue.Length Then
            charactersToShow = dialogue.Length
        End If
        Return dialogue.Substring(0, charactersToShow)
    End Function

    '// Commands (not literal) dialogue has a ! at the start
    Private Function IsCommand(toTest As String) As Boolean
        Return toTest(0) = "!"c
    End Function
End Class

