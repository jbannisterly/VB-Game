Option Strict On
Imports NEA.CoordDataTypes
Imports System.Runtime.InteropServices
'// Keep track of water ripples using a queue
Public Class RippleController
    Public Const MAX_RIPPLES As Integer = 8
    Private Const RIPPLE_DURATION As Single = 1
    Private rippleQueue(MAX_RIPPLES - 1) As Ripple
    Private headPtr As Integer = 0
    Private lastRipple As Single
    Sub New()
        For i = 0 To MAX_RIPPLES - 1
            rippleQueue(i) = New Ripple(New COORD2Sng(0, 0), Timer)
        Next
        lastRipple = CSng(Timer)
    End Sub

    '// Check if enough time has elapsed to add another ripple
    Public Function CanAddRipple() As Boolean
        Return Timer - lastRipple > RIPPLE_DURATION
    End Function

    '// Add a new ripple at a coordinate and move to next queue position
    Public Sub AddRipple(rippleLocation As COORD3Sng)
        lastRipple = CSng(Timer)
        '// Check if the ripple is in the water
        If rippleLocation.y < 40.1 Then
            rippleQueue(headPtr) = New Ripple(New COORD2Sng(rippleLocation.x, rippleLocation.z), Timer)
            headPtr += 1
            headPtr = headPtr Mod MAX_RIPPLES
        End If
    End Sub

    '// Return a pointer to memory containing ripple data
    Public Function GetRippleData() As IntPtr
        Dim rippleData(MAX_RIPPLES * 3 - 1) As Single
        Dim ripplePtr As IntPtr = Marshal.AllocHGlobal(MAX_RIPPLES * 12)
        '// Copy ripple data to array
        For i = 0 To MAX_RIPPLES - 1
            Array.Copy(rippleQueue(i).ToSingle, 0, rippleData, i * 3, 3)
        Next
        '// Copy ripple array to memory
        Marshal.Copy(rippleData, 0, ripplePtr, rippleData.Length)
        Return ripplePtr
    End Function

    '// Structure containing information about ripple location and time of ripple
    Structure Ripple
        Sub New(inCoord As COORD2Sng, inTime As Double)
            coord = inCoord
            startTime = inTime
        End Sub
        Public coord As COORD2Sng
        Public startTime As Double
        Function ToSingle() As Single()
            Dim data(2) As Single
            data(0) = coord.x
            data(1) = coord.y
            data(2) = CSng(Timer - startTime)
            Return data
        End Function
    End Structure
End Class

