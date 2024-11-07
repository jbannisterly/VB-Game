Option Strict On
'// Class to store current state of the OpenGL environment
'// Reduces number of state changes
Public Class OpenGLContext
    '// Variables to store current states
    Private currentVertexArray As UInteger
    Private currentProgram As UInteger
    Private currentTextures(15) As UInteger

    '// Check if vertex array to be bound is different
    Public Sub glBindVertexArray(VAO As UInteger)
        If currentVertexArray <> VAO Then
            OpenGLImporter.glBindVertexArray(VAO)
            currentVertexArray = VAO
        End If
    End Sub

    '// Check if current program needs to be changed
    Public Sub glUseProgram(program As UInteger)
        If currentProgram <> program Then
            OpenGLImporter.glUseProgram(program)
            currentProgram = program
        End If
    End Sub

    '// Minimise number of texture changes by checking if it needs to be changed
    Public Sub glBindTexture(target As UInteger, textureID As UInteger)
        OpenGLImporter.glActiveTexture(target)
        OpenGLImporter.glBindTexture(OpenGLImporter.GL_TEXTURE_2D, textureID)
        currentTextures(CInt(target - OpenGLImporter.GL_TEXTURE_0)) = textureID
    End Sub
End Class
