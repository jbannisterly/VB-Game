Option Strict On
Imports System.Runtime.InteropServices
'// Class to manage the keyboard input from the user
Public Class KeyboardInput

    '// Variable declaration of current keyboard state
    Public Shared KeysDown(128) As Boolean
    Private Shared KeysEnabled(128) As Boolean
    Public Shared keyBindLabels As String() = IO.File.ReadAllLines("Resources\KeyBindLabels.txt")

    '// Read user input and return a special character as necessary
    Public Shared Function GetSpecialCharacter(characterIn As String) As String
        Select Case characterIn
            Case " "
                Return "<SpaceCharacter>"
            Case Chr(16)
                Return "<ShiftCharacter>"
            Case Chr(18)
                Return "<AltCharacter>"
            Case Chr(27)
                Return "<EscapeCharacter>"
        End Select
        '// Key pressed is not a special character so return the original value
        Return characterIn
    End Function

    '// Overloads for checking if a key is down by ASCII code or by literal character
    Public Shared Function KeyDown(keyCode As Integer) As Boolean
        Return KeysDown(keyCode)
    End Function

    Public Shared Function KeyDown(key As Char) As Boolean
        Return KeysDown(Asc(key))
    End Function

    '// Update the keyboard input and check for toggled states once per frame
    Public Shared Sub NextFrame()
        For i = 0 To KeysDown.Length - 1
            KeysEnabled(i) = Not KeysDown(i)
            KeysDown(i) = GetKeyState(i) < 0
        Next
    End Sub

    '// Overoads for checking if a key was pressed in the previous frame
    Public Shared Function KeyPressed(keycode As Integer) As Boolean
        Return KeysDown(keycode) And KeysEnabled(keycode)
    End Function

    Public Shared Function KeyPressed(key As Char) As Boolean
        Return KeysDown(Asc(key)) And KeysEnabled(Asc(key))
    End Function

    '// Get an array of ASCII codes of keys that were pressed in the previous frame
    Public Shared Function GetKeysDown(ByRef keyPrevious As Boolean()) As Integer()
        Dim keysPressed As New List(Of Integer)
        Dim shift As Boolean = KeyDown(16)
        For i = 0 To keyPrevious.Length - 1
            If KeyDown(i) Then
                If Not keyPrevious(i) Then
                    '// Check for lower case
                    If Not shift And i >= 64 And i <= 90 Then
                        keysPressed.Add(i + 32)
                    Else
                        keysPressed.Add(i)
                    End If
                End If
                keyPrevious(i) = True
            Else
                keyPrevious(i) = False
            End If
        Next
        Return keysPressed.ToArray()
    End Function

    '// Class to store the current key bindings
    Public Class KeyBinding
        Private keyBinds As Integer()
        Public Function GetKeyBinds(index As Integer) As Integer
            Return keyBinds(index)
        End Function
        Public Sub SetKeyBinds(index As Integer, value As Integer)
            keyBinds(index) = value
            If Not IO.Directory.Exists("Settings") Then IO.Directory.CreateDirectory("Settings")
            SaveGame.WriteAllInt("Settings\Controls.settings", keyBinds)
        End Sub
        Sub New()
            If IO.Directory.Exists("Settings") AndAlso IO.File.Exists("Settings\Controls.settings") Then
                '// Load bindings from file
                keyBinds = SaveGame.ReadAllInt("Settings\Controls.settings")
            Else
                keyBinds = {87, 83, 65, 68, 32, 16, 18, 27}
                '// Load default keys
            End If
        End Sub
        Public Sub ResetInputs(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
            For i = 0 To keyBinds.Length - 1
                SetKeyBinds(i, {87, 83, 65, 68, 32, 16, 18, 27}(i))
            Next
        End Sub
    End Class

    '// Enum for key bind array indices
    Public Enum KeyBinds
        MoveForward = 0
        MoveBack = 1
        MoveLeft = 2
        MoveRight = 3
        Jump = 4
        Sprint = 5
        Dodge = 6
        Pause = 7
    End Enum

    '// Imported function to get whether a key is pressed
    <DllImport("User32.dll")>
    Private Shared Function GetKeyState(key As Integer) As Short
    End Function
End Class

