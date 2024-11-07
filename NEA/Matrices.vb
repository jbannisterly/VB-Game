Option Strict On
Imports System.Runtime.InteropServices
Imports NEA.CoordDataTypes
'// Class to handle matrix operations and represent a matrix
Public Class Matrices
    Public data As Single()
    Public width As Integer
    Public height As Integer

    Sub New(inH As Integer, inW As Integer, identity As Boolean)
        width = inW
        height = inH
        ReDim data(height * width - 1)
        '// Populate the matrix with a diagonal of 1s
        If identity Then
            For i = 0 To inH - 1
                data((inW + 1) * i) = 1
            Next
        End If
    End Sub

    '// Create a copy of matrices, since classes are reference by default
    Public Shared Function Copy(toCopy As Matrices()) As Matrices()
        Dim newCopy(toCopy.Length - 1) As Matrices
        For i = 0 To newCopy.Length - 1
            newCopy(i) = toCopy(i)
        Next
        Return newCopy
    End Function

    '// Get the average magnitude of a matrix
    Public Function ScaleApplied() As Single
        Dim scales As Single
        Dim averageScale As Single = 0

        For i = 0 To width - 2
            '// Ignore the translations
            scales = 0
            For j = 0 To height - 2
                scales += data(i * width + j) * data(i * width + j)
            Next
            averageScale += CSng(Math.Sqrt(scales) / (width - 1))
        Next
        Return averageScale
    End Function

    '// Swap the rows and columns of a matrix
    Public Sub Transpose()
        Dim newData(data.Length - 1) As Single
        '// Swap and store results in new array
        For i = 0 To width - 1
            For j = 0 To height - 1
                newData(i * width + j) = data(j * width + i)
            Next
        Next
        '// Copy new array to old array
        For i = 0 To data.Length - 1
            data(i) = newData(i)
        Next
    End Sub

    '// Copy an array of matrices to an address in memory, ensuring it can be read by OpenGL
    Public Shared Sub MatrixArrayToPtr(matrixPtr As IntPtr, arrMatrix As Matrices())
        For i = 0 To arrMatrix.Length - 1
            arrMatrix(i).ToOpenGlMatrix(matrixPtr, i)
        Next
    End Sub

    '// Multiply two matrices together
    Public Shared Function Multiply(matrixA As Matrices, matrixB As Matrices) As Matrices
        Dim newMatrix As New Matrices(matrixA.height, matrixB.width, False)
        Dim runningTotal As Single
        For i = 0 To newMatrix.height - 1
            For j = 0 To newMatrix.width - 1
                '// Calculate matrix product for each cell
                runningTotal = 0
                For k = 0 To matrixA.width - 1
                    runningTotal += matrixA.data(i * matrixA.width + k) * matrixB.data(k * matrixB.width + j)
                Next
                newMatrix.data(i * newMatrix.width + j) = runningTotal
            Next
        Next
        Return newMatrix
    End Function

    '// Copy matrix to memory, ensuring it can be read by OpenGL
    Public Function ToOpenGLMatrix() As IntPtr
        Dim resultPtr As IntPtr = Marshal.AllocHGlobal(width * height * 4)
        Dim result(data.Length - 1) As Single
        '// OpenGL matrices need to be transposed
        For i = 0 To height - 1
            For j = 0 To width - 1
                result(j * height + i) = data(i * width + j)
            Next
        Next
        Marshal.Copy(result, 0, resultPtr, result.Length)
        Return resultPtr
    End Function

    '// Create matrix at location in memory
    Public Function ToOpenGlMatrix(start As IntPtr, offset As Integer) As IntPtr
        Dim result(data.Length - 1) As Single
        For i = 0 To height - 1
            For j = 0 To width - 1
                result(j * height + i) = data(i * width + j)
            Next
        Next
        Marshal.Copy(result, 0, start + offset * 64, result.Length)
        Return start
    End Function

    '// Get the presepctive matrix
    Public Shared Function Perspective4() As Matrices
        Dim perspectiveMatrix As New Matrices(4, 4, False)
        perspectiveMatrix.data =
        {
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 0, -0.1,
        0, 0, 1, 0
        }
        '// OpenGL will divide the coordinates by the w value.
        '// So wNew = z
        '// zNew = -0.1 / z
        '// xNew = x / wNew = x / z
        '// yNew = y / wNew = y / z
        Return perspectiveMatrix
    End Function

    '// Overload for a translation matrix generator
    Public Shared Function Translate(ByVal coords As COORD3Sng, matrixToTranslate As Matrices) As Matrices
        Return Translate(coords.x, coords.y, coords.z, matrixToTranslate)
    End Function

    '// Generate a matrix representing a translation
    '// The w coordinate is 1 and used for constants
    Public Shared Function Translate(ByVal x As Single, ByVal y As Single, ByVal z As Single, matrixToTranslate As Matrices) As Matrices
        Dim translateMatrix As New Matrices(4, 4, True)
        translateMatrix.data(3) = x
        translateMatrix.data(7) = y
        translateMatrix.data(11) = z
        Return Multiply(translateMatrix, matrixToTranslate)
    End Function

    Public Shared Function Translate(ByVal translations As Single()) As Matrices
        Dim translateMatrix As New Matrices(4, 4, True)
        translateMatrix.data(3) = translations(0)
        translateMatrix.data(7) = translations(1)
        translateMatrix.data(11) = translations(2)
        Return translateMatrix
    End Function

    '// Generate a matrix representing a rotation about the y axis
    Public Shared Function RotateXZPlaneClockwise(ByVal theta As Single, matrixToRotate As Matrices) As Matrices
        Dim rotateMatrix As New Matrices(4, 4, True)
        '// Cache sin and cos values so the do not have to be recalculated
        Dim sin As Single = CSng(Math.Sin(theta))
        Dim cos As Single = CSng(Math.Cos(theta))
        rotateMatrix.data(0) = cos
        rotateMatrix.data(2) = -sin
        rotateMatrix.data(8) = sin
        rotateMatrix.data(10) = cos
        Return Multiply(rotateMatrix, matrixToRotate)
    End Function

    Public Shared Function RotatePitchUp(ByVal theta As Single, matrixToRotate As Matrices) As Matrices
        Dim rotateMatrix As New Matrices(4, 4, True)
        Dim sin As Single = CSng(Math.Sin(theta))
        Dim cos As Single = CSng(Math.Cos(theta))
        rotateMatrix.data(5) = cos
        rotateMatrix.data(6) = sin
        rotateMatrix.data(9) = -sin
        rotateMatrix.data(10) = cos
        Return Multiply(rotateMatrix, matrixToRotate)
    End Function

    Public Shared Function RotateRollClockwise(ByVal theta As Single, matrixToRotate As Matrices) As Matrices
        Dim rotateMatrix As New Matrices(4, 4, True)
        Dim sin As Single = CSng(Math.Sin(theta))
        Dim cos As Single = CSng(Math.Cos(theta))
        rotateMatrix.data(0) = cos
        rotateMatrix.data(1) = sin
        rotateMatrix.data(4) = -sin
        rotateMatrix.data(5) = cos
        Return Multiply(rotateMatrix, matrixToRotate)
    End Function

    '// Generate a matrix representing a scale in given dimensions
    Public Shared Function Scale(ByVal scales As Single()) As Matrices
        Dim scaleMatrix As New Matrices(4, 4, True)
        scaleMatrix.data(0) = scales(0)
        scaleMatrix.data(5) = scales(1)
        scaleMatrix.data(10) = scales(2)
        Return scaleMatrix
    End Function

    Public Shared Function ScaleXY(ByVal x As Single, ByVal y As Single, matrixToScale As Matrices) As Matrices
        Dim scaleMatrix As New Matrices(4, 4, True)
        scaleMatrix.data(0) = x
        scaleMatrix.data(5) = y
        Return Multiply(scaleMatrix, matrixToScale)
    End Function

    Public Shared Function ScaleXYZ(ByVal x As Single, ByVal y As Single, ByVal z As Single, matrixToScale As Matrices) As Matrices
        Dim scaleMatrix As New Matrices(4, 4, True)
        scaleMatrix.data(0) = x
        scaleMatrix.data(5) = y
        scaleMatrix.data(10) = z
        Return Multiply(scaleMatrix, matrixToScale)
    End Function

    Public Shared Function Scale(ByVal s As Single, matrixToScale As Matrices) As Matrices
        Return ScaleXYZ(s, s, s, matrixToScale)
    End Function

End Class

