Option Strict On
Public Class AnimationFrames
    Const FRAME_DIRECTORY As String = "Resources\AnimationFrames\"
    Const FRAMERATE As Single = 20
    Public frames As New List(Of AnimationFrame)

    Sub New(importPath As String)
        Dim rawData As String()
        If Not IO.File.Exists(FRAME_DIRECTORY & importPath & ".frame") Then
            importPath = "Default"
        End If
        rawData = IO.File.ReadAllLines(FRAME_DIRECTORY & importPath & ".frame")
        For i = 0 To rawData.Length - 1
            frames.Add(New AnimationFrame(rawData(i)))
        Next
    End Sub

    Public Function GetAnimationFrame(key As String) As AnimationFrame
        For i = 0 To frames.Count - 1
            If frames(i).key = key Then Return frames(i)
        Next
        '// Fallback if key is not valid
        Return New AnimationFrame("def:1,2,69")
    End Function

    Public Structure AnimationFrame
        Public key As String
        Public speed As Single
        Public data As Integer()
        Public attack As Integer
        Sub New(rawData As String)
            '// Import animation names to use from the items file
            '// Each item has references its own animations 
            Dim splitData As String() = rawData.Split(":"c)
            Dim start As Integer
            Dim finish As Integer
            key = splitData(0)
            splitData = splitData(1).Split(","c)
            speed = CSng(Val(splitData(0)))
            start = CInt(Val(splitData(1)))
            finish = CInt(Val(splitData(2)))
            ReDim data(finish - start)
            For i = 0 To data.Length - 1
                data(i) = i + start
            Next
            If splitData.Length > 3 Then
                attack = CInt(Val(splitData(3))) - start
            End If
        End Sub

        Public Function GetFrame(time As Single, offset As Integer) As Integer
            '// Returns index of frame, with wraparound
            While time < 0
                time += data.Length / FRAMERATE
            End While
            time = Int(time * FRAMERATE + offset) Mod data.Length
            Return data(CInt(time))
        End Function
    End Structure

    Public Shared Function InterpolationFactor(time As Single) As Single
        '// Linear interpolation between two frames, called by GLTF model
        Return (time * FRAMERATE) Mod 1
    End Function

End Class

