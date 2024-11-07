Option Strict On
Imports System.Runtime.InteropServices
Imports System.Console
'// Class to manage mouse movement and mouse clicks
Public Class MouseInput
    '// Variable declaration
    Public Shared MouseClick(2) As Boolean
    Public Shared MouseEnabled(2) As Boolean

    '// Get the distance in pixels the mouse has mobed since it was last called
    Public Shared Function DeltaMouse() As POINT
        Dim delta As New POINT
        Dim position As New POINT
        '// Get current position of the mouse
        GetCursorPos(position)
        '// Get the change in mouse position
        delta.x = position.x - 50
        delta.y = position.y - 50
        '// Set the position of the mouse so change can be calculated next time
        SetCursorPos(50, 50)
        Return delta
    End Function

    '// Sets the mouse cursor to 1 pixel big to hide it
    Public Shared Sub HideMouseCursor()
        SystemParametersInfoA(&H2029, 0, 1, 0)
    End Sub

    '// Resets the size of the mouse
    Public Shared Sub ShowMouseCursor()
        SystemParametersInfoA(&H2029, 0, 32, 0)
    End Sub

    '// Check if the user is holding the mouse button down
    Public Shared Function MouseDown(keyCode As Buttons) As Boolean
        Return GetKeyState(keyCode) < 0
    End Function

    '// Update mouse input data for the frame
    '// Prevents repeated calls to GetKeyState
    Public Shared Sub NextFrame()
        For i = 0 To 2
            '// Mouse click is only true for the frame it was clicked
            MouseClick(i) = MouseDown(CType(i, Buttons)) And MouseEnabled(i)
            MouseEnabled(i) = Not MouseDown(CType(i, Buttons))
        Next
    End Sub

    '// Hide the cursor and set the position
    Public Shared Sub Initialise()
        HideMouseCursor()
        SetCursorPos(50, 50)
    End Sub

    '// Structure declaration for imported function

    <StructLayout(LayoutKind.Sequential)>
    Structure POINT
        Public x As Int32
        Public y As Int32
    End Structure

    Public Enum Buttons
        Left = 1
        Right = 2
        Middle = 4
    End Enum

    '// Imported dlls to get inputs

    <DllImport("User32.dll")>
    Private Shared Function GetCursorPos(ByRef p As POINT) As Boolean
    End Function

    <DllImport("User32.dll")>
    Private Shared Function SetCursorPos(ByVal x As Int32, ByVal y As Int32) As Boolean
    End Function

    <DllImport("User32.dll")>
    Private Shared Function GetKeyState(key As Integer) As Short
    End Function

    <DllImport("User32.dll")>
    Private Shared Function SystemParametersInfoA(action As UInt32, param As UInt32, param2 As UInt32, ini As UInt32) As Boolean
    End Function
End Class

