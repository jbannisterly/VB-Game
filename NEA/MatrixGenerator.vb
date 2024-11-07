Option Strict On
Imports NEA.CoordDataTypes
'// Class to generate matrices from the position of a camera
Public Class MatrixGenerator
    '// Height of the water
    Public Const REFLECTION_HEIGHT As Single = 40.1

    '// Get matrices to tranform world space to screen space
    Public Shared Sub GeneratePlayerMatrices(ByRef matrixRelative As Matrices, ByRef matrixView As Matrices, ByRef matrixPerspective As Matrices, monitorSize As COORD2Short, character As Player, reflection As Boolean, cameraPosition As COORD3Sng, cameraRotation As Single, cameraElevation As Single)
        Dim yPosition As Single

        '// Camera position is beneath the water if rendering reflection
        If reflection Then
            yPosition = 2 * REFLECTION_HEIGHT - cameraPosition.y
        Else
            yPosition = cameraPosition.y
        End If

        '// Get relative position of the object
        matrixRelative = GetRelativeMatrix(cameraPosition.x, yPosition, cameraPosition.z)

        '// Transform the object so the camera is pointing at it
        matrixView = GetViewMatrix(matrixRelative, cameraRotation, cameraElevation * If(reflection, -1, 1))

        '// Transform to screen space
        matrixPerspective = GetPerspectiveMatrix(matrixView, monitorSize)

        '// Invert the image vertically if a reflection is being displayed
        If reflection Then
            matrixPerspective = Matrices.ScaleXYZ(1, -1, 1, matrixPerspective)
        End If
    End Sub

    '// Translate backwards
    Public Shared Function GetRelativeMatrix(x As Single, y As Single, z As Single) As Matrices
        Return Matrices.Translate({-x, -y, -z})
    End Function

    '// Rotate the view so it is aligned with the camera rotation
    Public Shared Function GetViewMatrix(matrixRelative As Matrices, rotation As Single, elevation As Single) As Matrices
        Dim matrixView As New Matrices(4, 4, True)

        matrixView = Matrices.RotateXZPlaneClockwise(rotation, matrixRelative)
        matrixView = Matrices.RotatePitchUp(elevation, matrixView)
        Return matrixView
    End Function

    '// Scale the view to the monitor and project to screen space
    Public Shared Function GetPerspectiveMatrix(matrixView As Matrices, monitorSize As COORD2Short) As Matrices
        Dim matrixPerspective As New Matrices(4, 4, True)
        matrixPerspective = Matrices.Perspective4()
        If monitorSize.x > monitorSize.y Then
            matrixPerspective = Matrices.ScaleXY(CSng(monitorSize.y) / monitorSize.x, 1, matrixPerspective)
        Else
            matrixPerspective = Matrices.ScaleXY(1, CSng(monitorSize.x) / monitorSize.y, matrixPerspective)
        End If
        matrixPerspective = Matrices.Multiply(matrixPerspective, matrixView)
        Return matrixPerspective
    End Function

End Class

