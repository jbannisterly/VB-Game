Option Strict On
'// Manage all NPC entities in the game and handle actions
Public Class NPCManager
    Private Const TALK_RANGE As Single = 2
    Private Const NPCPath As String = "Resources\NPC"
    '// Variable declaration
    Public character As Player
    Public NPCList As New List(Of NPC)
    Private audio As AudioManager
    Private lblDialogue As New GUIObject.Label
    Private enemies As EnemyManager
    Private talkTime As Single

    '// Initialise and set references
    Sub New(ByRef models As ModelManager, ByRef inAudio As AudioManager, ByRef inCharacter As Player, ByRef forms As GUIObject.Form(), ByRef inEnemies As EnemyManager)
        Dim NPCIdentifiers As String() = IO.Directory.GetFiles(NPCPath)
        Dim dialogueForm As GUIObject.Form
        audio = inAudio
        character = inCharacter
        enemies = inEnemies
        For i = 0 To NPCIdentifiers.Length - 1
            NPCIdentifiers(i) = NPCIdentifiers(i).Split("\"c)(NPCIdentifiers(i).Split("\"c).Length - 1)
            NPCIdentifiers(i) = NPCIdentifiers(i).Split("."c)(0)
        Next
        '// Load NPC data from a file
        LoadNPCs(NPCIdentifiers, models)
        '// Get GUI object references
        dialogueForm = forms(Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmTalk))
        dialogueForm.GetElementByID(Menu.Controls.ID_lblTalk, lblDialogue)
    End Sub

    '// Return an array of bytes to be written to a file
    Public Function GetNPCData() As Byte()
        Dim rawData As New List(Of Byte)
        For i = 0 To NPCList.Count - 1
            '// Concatenate all NPC save data
            rawData.AddRange(NPCList(i).GetSaveData())
        Next
        Return rawData.ToArray()
    End Function

    '// Load NPC data from a file
    Public Sub LoadNPCData(rawData As Byte())
        For i = 0 To NPCList.Count - 1
            NPCList(i).LoadFromSaveData(rawData, i * 8)
        Next
    End Sub

    '// Called when a new game is loaded
    '// Resets NPC progress
    Public Sub Initialise()
        For i = 0 To NPCList.Count - 1
            NPCList(i).Initialise()
        Next
    End Sub

    '// Call all respawn code for all NPCs
    Public Sub Respawn()
        For i = 0 To NPCList.Count - 1
            NPCList(i).Respawn()
        Next
    End Sub

    '// Change the dialogue text on the display so it types out
    Public Sub RefreshDialogue(deltaT As Single)
        talkTime += deltaT
        lblDialogue.text = NPCList(character.talkTarget).GetDialogue(talkTime)
    End Sub

    '// Get command from NPC the player is currently interacting with
    Public Function GetCommand() As String
        Return NPCList(character.talkTarget).GetFullDialogue
    End Function

    '// Clear GUI dialogue display
    Public Sub ClearDialogue()
        lblDialogue.text = ""
    End Sub

    '// Render NPCs to shadow map
    Public Sub CastShadows(ByRef shadowRenderer As Shadow, shadowModelProgram As Integer, shadowFramebuffer As UInteger, shadowTexturesDynamic As UInteger())
        For i = 0 To NPCList.Count - 1
            shadowRenderer.ProjectModelShadows(shadowTexturesDynamic, shadowModelProgram, shadowFramebuffer, NPCList(i), False, {New Matrices(4, 4, True)}, -1)
        Next
    End Sub

    '// Reset text type progress and move to next dialogue line
    Public Function AdvanceDialogue() As Boolean
        Dim finished As Boolean
        talkTime = 0
        finished = NPCList(character.talkTarget).AdvanceDialogue()
        Return finished
    End Function

    '// Called once per frame
    Public Sub Update(deltaT As Single)
        '// Get closest NPC and set flag of NPC
        Dim closestNPC As Integer = GetClosestNPC()
        For i = 0 To NPCList.Count - 1
            If i = closestNPC Then
                NPCList(i).closest = True
            Else
                NPCList(i).closest = False
            End If
            '// Update rotation and animation
            NPCList(i).Update(deltaT)
        Next
    End Sub

    '// Returns index of NPC that the player will interact with next
    Public Function GetClosestNPC() As Integer
        Dim angle As Single = 0
        Dim closestAngle As Single = 1000000
        Dim closestNPC As Integer = -1
        For i = 0 To NPCList.Count - 1
            '// NPC must be close enough to talk to
            If NPCList(i).InRange(character.location, character.cameraRotation, Player.TALK_RANGE) Then
                '// Get NPC in range and directly in front of player
                angle = NPCList(i).GetAngle(character.location, character.cameraRotation)
                If angle < closestAngle Then
                    closestAngle = angle
                    closestNPC = i
                End If
            End If
        Next
        If closestAngle <= 1000 Then
            '// Found talkable NPC
            Return closestNPC
        Else
            '// No NPC in range
            Return -1
        End If
    End Function

    '// Call display on all NPCs
    Public Sub RenderNPCs(program As UInteger, ByRef matrixRelative As Matrices, ByRef matrixView As Matrices, ByRef matrixPerspective As Matrices)
        For i = 0 To NPCList.Count - 1
            NPCList(i).Display(program, matrixRelative, matrixView, matrixPerspective, False, {New Matrices(4, 4, True)}, -1)
        Next
    End Sub

    '// Load all default NPCs
    Private Sub LoadNPCs(NPCtypes As String(), ByRef models As ModelManager)
        Dim NPCInstance As NPC
        Dim NPCData As String()
        Dim startLocation As String()
        For i = 0 To NPCtypes.Length - 1
            NPCData = IO.File.ReadAllLines(NPCPath & "\" & NPCtypes(i) & ".txt")
            startLocation = NPCData(0).Split(" "c)
            '// Initialise NPC
            NPCInstance = New NPC(models.GetModel("PlayerModel2"), audio, NPCData, NPCtypes(i), enemies, character)
            '// Set location
            NPCInstance.location = New CoordDataTypes.COORD3Sng(CSng(startLocation(0)), CSng(startLocation(1)), CSng(startLocation(2)))
            NPCInstance.location.y = PerlinNoise.GetHeight(NPCInstance.location.x, NPCInstance.location.z, GameWorld.randoms) * 100
            '// NPCs are dwarves
            NPCInstance.size = New CoordDataTypes.COORD3Sng(1, 0.7, 0.8)
            NPCInstance.animationName = "idle"
            NPCList.Add(NPCInstance)
        Next
    End Sub

End Class

