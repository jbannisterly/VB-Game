Option Strict On
Imports System.Runtime.InteropServices
Imports NEA.CoordDataTypes
'// Manage the console window
Public Class Window
    Const PROCESSED_INPUT As UInt32 = 1
    Const ECHO_INPUT As UInt32 = 4
    Const MOUSE_INPUT As UInt32 = 16
    Const QUICK_EDIT As UInt32 = 64

    '// By default the console will pause when the user clicks on it
    Public Shared Sub DisableInput()
        Dim consoleMode As UInt32
        Dim hnd As IntPtr
        '// Get handle to console
        hnd = GetStdHandle(-10)
        '// Remove click pause features as well as ctrl-c to exit
        GetConsoleMode(hnd, consoleMode)
        consoleMode = consoleMode And Not (QUICK_EDIT) And Not (ECHO_INPUT) And Not (PROCESSED_INPUT) Or (MOUSE_INPUT)
        SetConsoleMode(hnd, consoleMode)
    End Sub

    '// Make the console full screen
    Public Shared Sub FullScreen()
        Dim size As New COORD2Short
        Dim hnd As IntPtr = GetStdHandle(-11)
        Dim fail As Boolean = False
        Console.Clear()
        '// Remove side scrollbar by shrinking the buffer height as far as possible
        Do Until fail
            Try
                Console.BufferHeight \= 2
            Catch
                fail = True
            End Try
        Loop
        '// Fullscreen console
        SetConsoleDisplayMode(hnd, 1, size)
    End Sub

    '// Get the size of the console in pixels
    Public Shared Function GetSize() As COORD2Short
        Dim size As New COORD2Short
        size.x = CShort(GetSystemMetrics(0))
        size.y = CShort(GetSystemMetrics(1))
        Return size
    End Function

    '// Imported DLL functions

    <DllImport("Kernel32.dll")>
    Private Shared Function GetConsoleMode(ByVal hnd As IntPtr, ByRef mode As UInt32) As Boolean
    End Function

    <DllImport("Kernel32.dll")>
    Private Shared Function SetConsoleMode(ByVal hnd As IntPtr, ByVal mode As UInt32) As Boolean
    End Function

    <DllImport("User32.dll")>
    Private Shared Function GetSystemMetrics(ByVal index As Int32) As Int32
    End Function

    <DllImport("Kernel32.dll")>
    Private Shared Function GetStdHandle(ByVal handle As Int32) As IntPtr
    End Function

    <DllImport("Kernel32.dll")>
    Private Shared Function SetConsoleDisplayMode(ByVal hnd As IntPtr, ByVal flags As UInt32, ByRef dimensions As COORD2Short) As Boolean
    End Function
End Class


