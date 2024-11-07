Option Strict On
'// Class to manage quaternions (represent rotations)
Public Class Quaternion
    '// Linearly interpolate between quaternions and normalise the result
    Public Shared Function NLerp(quaternions As Quaternion(), weights As Single()) As Quaternion
        Dim sumQuart As New Quaternion

        '// Ensure all quaternions are in the same direction (not backwards)
        If quaternions.Length > 1 Then
            For i = 1 To quaternions.Length - 1
                If Dot(quaternions(0), quaternions(i)) < 0 Then
                    quaternions(i).W *= -1
                    quaternions(i).X *= -1
                    quaternions(i).Y *= -1
                    quaternions(i).Z *= -1
                End If
            Next
        End If

        '// Take a weighted sum of quaternions
        For i = 0 To quaternions.Length - 1
            sumQuart.W += quaternions(i).W * weights(i)
            sumQuart.X += quaternions(i).X * weights(i)
            sumQuart.Y += quaternions(i).Y * weights(i)
            sumQuart.Z += quaternions(i).Z * weights(i)
        Next

        '// Normalise the result
        Return Normalise(sumQuart)
    End Function

    '// Calculate the dot product of two quaternions
    Public Shared Function Dot(quartA As Quaternion, quartB As Quaternion) As Single
        Return quartA.W * quartB.W + quartA.X * quartB.X + quartA.Y * quartB.Y + quartA.Z * quartB.Z
    End Function

    '// Ensure the magnitude of a quaternion is 1
    Public Shared Function Normalise(toNormalise As Quaternion) As Quaternion
        Dim mag As Single = Magnitude(toNormalise)
        toNormalise.W /= mag
        toNormalise.X /= mag
        toNormalise.Y /= mag
        toNormalise.Z /= mag
        Return toNormalise
    End Function

    '// Get length of a quaternion
    Public Shared Function Magnitude(quat As Quaternion) As Single
        Return CSng(Math.Sqrt(quat.W * quat.W + quat.X * quat.X + quat.Y * quat.Y + quat.Z * quat.Z))
    End Function

    '// Overload to convert a quaternion to a matrix
    Public Shared Function QuarternionToMatrix(w As Single, x As Single, y As Single, z As Single) As Matrices
        Dim quart As New Quaternion
        quart.W = w
        quart.X = x
        quart.Y = y
        quart.Z = z
        Return QuarternionToMatrix(quart)
    End Function

    '// Convert a quaternion to a rotation matrix
    Public Shared Function QuarternionToMatrix(quartIn As Quaternion) As Matrices
        Dim matrixOut As New Matrices(4, 4, True)
        Dim XX, YY, ZZ, XY, XZ, XW, YZ, YW, ZW As Single
        '// To apply a quaternion, do QPQ*
        '// From this, the equivalent matrix can be derived

        '// Precompute products of quaternion elements
        XX = quartIn.X * quartIn.X * 2
        YY = quartIn.Y * quartIn.Y * 2
        ZZ = quartIn.Z * quartIn.Z * 2
        XY = quartIn.X * quartIn.Y * 2
        XZ = quartIn.X * quartIn.Z * 2
        XW = quartIn.X * quartIn.W * 2
        YZ = quartIn.Y * quartIn.Z * 2
        YW = quartIn.Y * quartIn.W * 2
        ZW = quartIn.Z * quartIn.W * 2

        '// Set matrix values
        matrixOut.data(0) = 1 - YY - ZZ
        matrixOut.data(1) = XY - ZW
        matrixOut.data(2) = XZ + YW
        matrixOut.data(4) = XY + ZW
        matrixOut.data(5) = 1 - XX - ZZ
        matrixOut.data(6) = YZ - XW
        matrixOut.data(8) = XZ - YW
        matrixOut.data(9) = YZ + XW
        matrixOut.data(10) = 1 - XX - YY
        Return matrixOut
    End Function

    '// Structure declaration

    Structure Quaternion
        Public W As Single
        Public X As Single
        Public Y As Single
        Public Z As Single
    End Structure
End Class

