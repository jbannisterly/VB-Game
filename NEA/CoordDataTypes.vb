Option Strict On
Imports System.Runtime.InteropServices
Public Class CoordDataTypes

    '// Helper function to calculate distance bewteen two points
    Public Shared Function Distance(vectorA As COORD3Sng, vectorB As COORD3Sng) As Single
        Dim difference As New COORD3Sng(vectorA.x - vectorB.x, vectorA.y - vectorB.y, vectorA.z - vectorB.z)
        Return CSng(Math.Sqrt(difference.x * difference.x + difference.y * difference.y + difference.z * difference.z))
    End Function

    '// Helper function to calculate angle between three points
    Public Shared Function Theta(origin As COORD3Sng, coordA As COORD3Sng, coordB As COORD3Sng) As Single
        Dim vectorOrigin As Single() = Vector.Vectorify(origin)
        Dim vectorA As Single() = Vector.Vectorify(coordA)
        Dim vectorB As Single() = Vector.Vectorify(coordB)
        vectorA = Vector.AtoB(vectorOrigin, vectorA)
        vectorB = Vector.AtoB(vectorOrigin, vectorB)
        Return CSng(Math.Acos(Vector.Dot(vectorA, vectorB) / (Vector.GetMagnitude(vectorA) * Vector.GetMagnitude(vectorB))))
    End Function

    '// Structure declaration of coordinate data types

    <StructLayout(LayoutKind.Sequential)>
    Public Structure COORD2Short
        Public x As Short
        Public y As Short
    End Structure

    Public Structure COORD2Sng
        Sub New(inX As Single, inY As Single)
            x = inX
            y = inY
        End Sub
        Public x As Single
        Public y As Single
    End Structure

    Public Structure COORD3Sng
        Public x As Single
        Public y As Single
        Public z As Single
        Sub New(inX As Single, inY As Single, inZ As Single)
            x = inX
            y = inY
            z = inZ
        End Sub
    End Structure

End Class

