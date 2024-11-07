Option Strict On
'// Class to handle vector operations
'// Vectors are stored as an array of floating point numbers
Public Class Vector
    '// Calculate the cross product of two vectors
    Public Shared Function Cross(vectorA As Single(), vectorB As Single()) As Single()
        Return {vectorA(1) * vectorB(2) - vectorA(2) * vectorB(1), vectorA(2) * vectorB(0) - vectorA(0) * vectorB(2), vectorA(0) * vectorB(1) - vectorA(1) * vectorB(0)}
    End Function

    '// Nomrmalise a vector
    Public Shared Function Normalise(toNormalise As Single()) As Single()
        Dim magnitude As Single = GetMagnitude(toNormalise)
        '// Divide each component by the magnitude of the vector
        For i = 0 To toNormalise.Length - 1
            toNormalise(i) /= magnitude
        Next
        Return toNormalise
    End Function

    '// Convert a 3D coord data type to vector
    Public Shared Function Vectorify(data As CoordDataTypes.COORD3Sng) As Single()
        Return {data.x, data.y, data.z}
    End Function

    '// Calculate the dot product of two vectors
    Public Shared Function Dot(vectorA As Single(), vectorB As Single()) As Single
        Dim dotProduct As Single = 0
        For i = 0 To vectorA.Length - 1
            dotProduct += vectorA(i) * vectorB(i)
        Next
        Return dotProduct
    End Function

    '// Get the vector from tip of vector A to tip of vector B if both tails are the same
    Public Shared Function AtoB(vectorA As Single(), vectorB As Single()) As Single()
        Dim vectorAtoB(vectorA.Length - 1) As Single
        For i = 0 To vectorAtoB.Length - 1
            vectorAtoB(i) = vectorB(i) - vectorA(i)
        Next
        Return vectorAtoB
    End Function

    '// Get length of a vector using Pythagoras
    Public Shared Function GetMagnitude(vectorIn As Single()) As Single
        Dim magnitude As Single = 0
        For i = 0 To vectorIn.Length - 1
            magnitude += vectorIn(i) * vectorIn(i)
        Next
        Return CSng(Math.Sqrt(magnitude))
    End Function
End Class

