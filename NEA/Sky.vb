Option Strict On
Imports NEA.OpenGLImporter
Imports System.Runtime.InteropServices
Imports NEA.CoordDataTypes
'// Class to draw a sky cubemap
Public Class Sky
    '// Indexed vertices for a cube
    Const strIndices As String = "012132456576406026517137014154236376"
    Const strVertices As String = "000001010011100101110111"

    Public Shared buffersPtr As IntPtr = Marshal.AllocHGlobal(8)
    Public Shared texPtr As IntPtr
    Public Shared vertices As IntPtr
    Public Shared context As OpenGLContext
    Public Shared outputTexture As UInteger
    Private Shared outputDepth As UInteger
    Private Shared framebuffer As UInteger

    '// Intialise texture target for sky
    Public Shared Sub LoadOutputTexture()
        Dim framePtr As IntPtr = Marshal.AllocHGlobal(4)
        Dim targetPtr As IntPtr = Marshal.AllocHGlobal(4)
        '// Generate framebuffer target
        glGenFramebuffers(1, framePtr)
        framebuffer = CUInt(Marshal.ReadInt32(framePtr))
        '// Generate sky textures
        glGenTextures(2, targetPtr)
        outputTexture = CUInt(Marshal.ReadInt32(targetPtr))
        outputDepth = CUInt(Marshal.ReadInt32(targetPtr + 4))
        glBindFramebuffer(GL_FRAMEBUFFER, framebuffer)
        OpenGLWrapper.WriteToTextureInit(context, outputTexture, Window.GetSize().x, Window.GetSize().y, GL_COLOR_ATTACHMENT, GL_RGB, True)
        OpenGLWrapper.WriteToTextureInit(context, outputDepth, Window.GetSize().x, Window.GetSize().y, GL_DEPTH_ATTACHMENT, GL_DEPTH_COMPONENT, True)

        Marshal.FreeHGlobal(framePtr)
        Marshal.FreeHGlobal(targetPtr)
    End Sub

    '// Create vertex array object to be used so sky does not overwrite others
    Public Shared Function GenerateVAO() As UInt32
        '// Get vertex data
        Dim vaoPtr As IntPtr = Marshal.AllocHGlobal(4)
        Dim vao As UInt32
        Dim vertArr As Single() = GenerateVertices()
        Dim indexArr As Integer() = GenerateIndices()
        '// Generate buffers
        glGenVertexArrays(1, vaoPtr)
        glGenBuffers(2, buffersPtr)
        vao = CUInt(Marshal.ReadInt32(vaoPtr))
        context.glBindVertexArray(vao)
        '// Copy vertex data to buffers
        OpenGLWrapper.BufferData(vertArr, GL_ARRAY_BUFFER, 0, buffersPtr)
        OpenGLWrapper.BufferData(indexArr, GL_ELEMENT_ARRAY_BUFFER, 1, buffersPtr)
        '// Bind buffers to inputs
        glEnableVertexAttribArray(0)
        glBindBuffer(GL_ARRAY_BUFFER, CUInt(Marshal.ReadInt32(buffersPtr)))
        glVertexAttribPointer(0, 3, GL_FLOAT, False, 0, 0)
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, CUInt(Marshal.ReadInt32(buffersPtr + 4)))
        Return vao
    End Function

    '// Convert string of vertices to array of floats
    Private Shared Function GenerateVertices() As Single()
        Dim vertices(strVertices.Length - 1) As Single
        For i = 0 To strVertices.Length - 1
            If strVertices(i) = "0" Then
                vertices(i) = -1
            Else
                vertices(i) = 1
            End If
        Next
        Return vertices
    End Function

    '// Convert string of indices to index array
    Private Shared Function GenerateIndices() As Integer()
        Dim indices(strIndices.Length - 1) As Integer
        For i = 0 To strIndices.Length - 1
            indices(i) = Asc(strIndices(i)) - Asc("0")
        Next
        Return indices
    End Function

    '// Returns a matrix to transform from screen space to relative space
    Public Shared Function GetSkyMatrix(size As COORD2Short, playerElevation As Single, playerRotation As Single, reflection As Boolean) As Matrices
        Dim skyMatrix As New Matrices(4, 4, True)
        '// Generate reverse matrices in the opposite order to the usual way
        If size.x < size.y Then
            skyMatrix = Matrices.ScaleXY(1, CSng(size.y / size.x), skyMatrix)
        Else
            skyMatrix = Matrices.ScaleXY(CSng(size.x / size.y), 1, skyMatrix)
        End If
        skyMatrix = Matrices.RotatePitchUp(-playerElevation, skyMatrix)
        skyMatrix = Matrices.RotateXZPlaneClockwise(-playerRotation, skyMatrix)
        If reflection Then skyMatrix = Matrices.ScaleXYZ(1, -1, 1, skyMatrix)
        '// There is no translation matrix as the sky can be assumed to be infinitely far away
        Return skyMatrix
    End Function

    '// Render sky to texture
    Public Shared Sub DisplaySky(playerElevation As Single, playerRotation As Single, size As COORD2Short, skyProgram As UInt32, cubeMap As UInt32, sunCoords As Single(), timeOfDay As Single, reflection As Boolean, saturation As Single, rain As Boolean)
        Dim skyMatrix As Matrices = GetSkyMatrix(size, playerElevation, playerRotation, reflection)
        '// Bind buffers and textures to be used
        vertices = InitialiseVertices()
        glBindFramebuffer(GL_FRAMEBUFFER, framebuffer)
        context.glUseProgram(skyProgram)
        context.glBindVertexArray(0)
        glBindBuffer(GL_ARRAY_BUFFER, 0)
        glActiveTexture(GL_TEXTURE_0 + 5)
        glEnable(GL_TEXTURE_2D)
        glBindTexture(GL_TEXTURE_CUBE_MAP, cubeMap)
        '// Set uniforms
        glUniform1i(glGetUniformLocationStr(CInt(skyProgram), "testCube"), 5)
        glUniform1f(glGetUniformLocationStr(CInt(skyProgram), "time"), timeOfDay)
        glUniformMatrix4fv(glGetUniformLocationStr(CInt(skyProgram), "skyMatrix"), 1, False, skyMatrix.ToOpenGLMatrix)
        glUniform3f(glGetUniformLocationStr(CInt(skyProgram), "sun"), sunCoords(0), sunCoords(1), sunCoords(2))
        glUniform3f(glGetUniformLocationStr(CInt(skyProgram), "moon"), -sunCoords(0), -sunCoords(1), -sunCoords(2))
        glUniform1f(glGetUniformLocationStr(CInt(skyProgram), "saturation"), If(rain, Math.Min(0.1F, saturation), saturation))
        glUniform1i(glGetUniformLocationStr(CInt(skyProgram), "rain"), If(rain, 1, 0))
        glEnableClientState(GL_VERTEX_ARRAY)
        '// Render sky
        glVertexPointer(3, GL_FLOAT, 0, vertices)
        glDrawArrays(GL_TRIANGLES, 0, 6)
    End Sub

    '// Copy vertices of a cube to memory
    Private Shared Function InitialiseVertices() As IntPtr
        Dim verticesData As Single() = {-1, -1, 1, 1, -1, 1, 1, 1, 1, -1, -1, 1, 1, 1, 1, -1, 1, 1}
        Dim vertPtr As IntPtr = Marshal.AllocHGlobal(verticesData.Length * 4)
        Marshal.Copy(verticesData, 0, vertPtr, verticesData.Length)
        Return vertPtr
    End Function
End Class

