Option Strict On
Imports NEA.OpenGLImporter
Imports System.Runtime.InteropServices
Imports NEA.CoordDataTypes
'// Handles the rendering of the terrain to a texture
Public Class TerrainRenderer
#Const DEBUG = False
    Public Shared texturesToBind As UInteger()
    Public Shared context As OpenGLContext

    '// Render terrain to texture
    Public Shared Function DisplayTerrain(cameraPosition As COORD3Sng, chunkBuffer As UInt32, cameraRotation As Single) As Integer
        Dim chunksToShow As Single()
        '// Cull chunks that are behind the camera
        chunksToShow = Instances(CHUNK_SIZE - 1, cameraPosition, cameraRotation, Math.PI / 2, RENDER_DISTANCE)
        '// Load buffer and texture data
        OpenGLWrapper.BufferData(chunksToShow, GL_ARRAY_BUFFER, chunkBuffer, True)
        TextureLoader.BindTextures(texturesToBind)
        '// Draw instanced elements
        '// Draws one chunk per instance
        glDrawElementsInstanced(GL_TRIANGLES, CHUNK_INDEX_LENGTH, GL_UNSIGNED_INT, 0, chunksToShow.Length \ 2)
        Return chunksToShow.Length \ 2
    End Function

    '// Initialise textures
    Public Shared Sub BindTiledTextures(terrainProgram As Int32, skyTexture As UInteger)
        Dim texPtr As IntPtr = Marshal.AllocHGlobal(12)
        '// Load textures from files to texture on GPU
        glGenTextures(3, texPtr)
        TextureLoader.LoadTexture2D("Grass", CUInt(Marshal.ReadInt32(texPtr)), GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)
        TextureLoader.LoadTexture2D("GrassNormal", CUInt(Marshal.ReadInt32(texPtr + 4)), GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)

        '// Bind textures to program samplers
        context.glUseProgram(CUInt(terrainProgram))
        context.glBindTexture(GL_TEXTURE_0, CUInt(Marshal.ReadInt32(texPtr)))
        glUniform1i(glGetUniformLocationStr(terrainProgram, "ground"), 0)
        context.glBindTexture(GL_TEXTURE_0 + 1, CUInt(Marshal.ReadInt32(texPtr + 4)))
        glUniform1i(glGetUniformLocationStr(terrainProgram, "groundNormal"), 1)

        If IsNothing(texturesToBind) OrElse texturesToBind.Length = 0 Then
            texturesToBind = {CUInt(Marshal.ReadInt32(texPtr)), CUInt(Marshal.ReadInt32(texPtr + 4))}
        End If

        Marshal.FreeHGlobal(texPtr)
    End Sub

    '// Bind heightmap to samplers of terrain renderer program
    Public Shared Sub BindHeightMap(shadowTextures As UInt32(), terrainGenerator As TerrainGenerator, terrainProgram As Int32)
        context.glBindTexture(GL_TEXTURE_0 + 3, terrainGenerator.textureID)
        glUniform1i(glGetUniformLocationStr(terrainProgram, "heightmap"), 3)
        context.glBindTexture(GL_TEXTURE_0 + 4, terrainGenerator.normalTextureID)
        glUniform1i(glGetUniformLocationStr(terrainProgram, "normalmap"), 4)

        texturesToBind = {texturesToBind(0), texturesToBind(1), 0, terrainGenerator.textureID, terrainGenerator.normalTextureID}
    End Sub

    '// Binds shadow map textures
    Public Shared Sub BindShadowMap(shadowTextures As UInt32(), terrainProgram As Int32)
        Dim shadowLocationsPtr As IntPtr = Marshal.AllocHGlobal(shadowTextures.Length * 4)
        context.glUseProgram(CUInt(terrainProgram))
        For i = 0 To shadowTextures.Length - 1
            context.glBindTexture(CUInt(GL_TEXTURE_0 + 8 + i), shadowTextures(i))
            Marshal.WriteInt32(shadowLocationsPtr + i * 4, 8 + i)
        Next
        glUniform1iv(glGetUniformLocationStr(terrainProgram, "shadow"), shadowTextures.Length, CInt(shadowLocationsPtr))
        glActiveTexture(GL_TEXTURE_0 + 3)
        glEnable(GL_TEXTURE_2D)
        Marshal.FreeHGlobal(shadowLocationsPtr)
    End Sub

    '// Create buffers and populate with vertices that make a grid
    Public Shared Sub InitialiseBuffers(bufferPtr As IntPtr, vertices As Single(), indices As Integer(), ByRef vertexBuffer As UInt32, ByRef chunkBuffer As UInt32)
        '// Generate buffers and populate
        glGenBuffers(3, bufferPtr)
        OpenGLWrapper.BufferData(vertices, GL_ARRAY_BUFFER, 0, bufferPtr)
        OpenGLWrapper.BufferData({0F, 0F, 10.0F, 10.0F}, GL_ARRAY_BUFFER, 1, bufferPtr, True)
        OpenGLWrapper.BufferData(indices, GL_ELEMENT_ARRAY_BUFFER, 2, bufferPtr)

        '// Bind buffers to shader input
        glEnableVertexAttribArray(0)
        glEnableVertexAttribArray(1)
        glBindBuffer(GL_ARRAY_BUFFER, CUInt(Marshal.ReadInt32(bufferPtr)))
        glVertexAttribPointer(0, 3, GL_FLOAT, False, 0, 0)
        glBindBuffer(GL_ARRAY_BUFFER, CUInt(Marshal.ReadInt32(bufferPtr + 4)))
        glVertexAttribPointer(1, 2, GL_FLOAT, False, 0, 0)
        glVertexAttribDivisor(1, 1)
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, CUInt(Marshal.ReadInt32(bufferPtr + 8)))
        vertexBuffer = CUInt(Marshal.ReadInt32(bufferPtr))
        chunkBuffer = CUInt(Marshal.ReadInt32(bufferPtr + 4))
    End Sub

    '// Generate an array of indices to create a grid
    Public Shared Sub GridVerticesIndices(ByRef vertices As Single(), ByRef indices As Integer(), size As Integer)
        Dim triangleOffset As Integer() = {0, 1, size, 1, size + 1, size}
        GridVertices(vertices, size)

        For i = 0 To size - 2
            For j = 0 To size - 2
                '// Create a square with 6 vertices
                For k = 0 To 5
                    indices((i * (size - 1) + j) * 6 + k) = triangleOffset(k) + i * size + j
                Next
            Next
        Next

    End Sub

    '// Create an array of vertices in a grid
    Public Shared Sub GridVertices(ByRef vertices As Single(), size As Integer)
        For i = 0 To size - 1
            For j = 0 To size - 1
                vertices((i * size + j) * 3) = i
                vertices((i * size + j) * 3 + 2) = j
            Next
        Next
    End Sub

    '// Load matrices to uniforms
    Public Shared Sub LoadMatrices(preProgram As Integer, postProgram As Integer, matrixPerspective As Matrices, matrixView As Matrices, matrixRelative As Matrices, lightSource As COORD3Sng, shadowFocusPoint As COORD2Sng, playerLocation As COORD3Sng, cameraLocation As COORD3Sng, reflection As Boolean)
        glUniformMatrix4fv(glGetUniformLocationStr(preProgram, "perspectiveMatrix"), 1, False, matrixPerspective.ToOpenGLMatrix())
        glUniformMatrix4fv(glGetUniformLocationStr(preProgram, "viewMatrix"), 1, False, matrixView.ToOpenGLMatrix())
        glUniformMatrix4fv(glGetUniformLocationStr(preProgram, "relativeMatrix"), 1, False, matrixRelative.ToOpenGLMatrix())
        glUniform1i(glGetUniformLocationStr(preProgram, "reflection"), If(reflection, 1, 0))
    End Sub

    '// Load uniform data
    Public Shared Sub LoadUniformsDeferred(postProgram As Integer, lightSource As COORD3Sng, shadowFocusPoint As COORD2Sng, playerLocation As COORD3Sng, cameraLocation As COORD3Sng, cameraRotation As Single, totT As Single, dayTime As Single, reflection As Boolean, splashes As IntPtr, saturation As Single)
        Dim lightColour As New COORD3Sng
        Dim sunset As Single = CSng(Math.Cos(dayTime))
        lightColour.x = 1
        lightColour.y = 1
        lightColour.z = 1

        '// Recolour light to orange if in sunset or sunrise
        If sunset < 0.14 Then
            lightColour.x = CSng(sunset ^ 0.15F) * 1.1F + 0.1F
            lightColour.y = sunset * 3.6F + 0.1F
            lightColour.z = sunset * sunset * 19.2F - sunset * 2 + 0.3F

            lightColour.x = 0.8
            lightColour.y = 0.3
            lightColour.z = 0.3
        End If

        '// Clamp light colour to minimum
        If lightColour.y < 0 Then lightColour.y = 0
        If lightColour.z < 0 Then lightColour.z = 0

        '// Dark blue light at night
        If sunset < -0.07 Then
            lightColour.x = 0.1
            lightColour.y = 0.1
            lightColour.z = 0.3
        End If

        '// Debug current light colour
#If DEBUG Then
        If KeyboardInput.KeyDown("Q"c) Then
            Console.SetCursorPosition(0, 1)
            Console.WriteLine(lightColour.x)
            Console.WriteLine(lightColour.y)
            Console.WriteLine(lightColour.z)
            Console.ReadLine()
        End If
#End If

        '// Load uniform data
        context.glUseProgram(CUInt(postProgram))
        glUniform3f(glGetUniformLocationStr(postProgram, "light"), lightSource.x, lightSource.y, lightSource.z)
        glUniform3f(glGetUniformLocationStr(postProgram, "lightColour"), lightColour.x, lightColour.y, lightColour.z)
        glUniform1f(glGetUniformLocationStr(postProgram, "timeOfDay"), dayTime)
        glUniform2f(glGetUniformLocationStr(postProgram, "focusPoint"), shadowFocusPoint.x, shadowFocusPoint.y)
        glUniform1f(glGetUniformLocationStr(postProgram, "inputValue"), totT)
        glUniform3f(glGetUniformLocationStr(postProgram, "playerPos"), cameraLocation.x, cameraLocation.y, cameraLocation.z)
        glUniform1f(glGetUniformLocationStr(postProgram, "playerRot"), cameraRotation)
        glUniform3fv(glGetUniformLocationStr(postProgram, "splash"), 24, splashes)
        glUniform1i(glGetUniformLocationStr(postProgram, "reflection"), If(reflection, 1, 0))
        glUniform1f(glGetUniformLocationStr(postProgram, "saturation"), saturation)
    End Sub

    '// Check if a chunk is visible from the current position of the player
    Private Shared Function ShouldDisplayChunk(chunkLocation As COORD2Sng, playerLocation As COORD2Sng, playerAngle As Single, fov As Single) As Boolean
        Dim pointLocation As New COORD2Sng()
        Dim offsetX As Single() = {0, 0, 1, 1}
        Dim offsetZ As Single() = {0, 1, 0, 1}
        Dim clock As Boolean = False
        Dim anticlock As Boolean = False
        Dim pointPosition As RelativePointPosition
        For i = 0 To offsetX.Length - 1
            '// Check each vertex of the chunk
            pointLocation.x = chunkLocation.x + offsetX(i)
            pointLocation.y = chunkLocation.y + offsetZ(i)
            pointPosition = GetPointPosition(pointLocation, playerLocation, playerAngle, fov)
            '// Check if point is within the field of view
            If pointPosition = RelativePointPosition.inside Then Return True
            '// If two vertices are either side of the view vector, display the chunk
            '// This is because sometimes the field of view is entirely within the chunk
            clock = clock Or pointPosition = RelativePointPosition.clock
            anticlock = anticlock Or pointPosition = RelativePointPosition.anticlock
            If clock And anticlock Then Return True
        Next
        Return False
    End Function

    '// Check where a point is relative to the field of view
    Private Shared Function GetPointPosition(point As COORD2Sng, playerLocation As COORD2Sng, playerAngle As Single, fov As Single) As RelativePointPosition
        '// Get angle from view vector
        Dim rawAngle As Single = CSng(Math.Atan((point.x - playerLocation.x) / (point.y - playerLocation.y)))
        Dim relativeAngle As Single = rawAngle - playerAngle + CSng(Math.PI) * 4
        If point.y < playerLocation.y Then relativeAngle += CSng(Math.PI)
        relativeAngle = relativeAngle Mod (CSng(Math.PI) * 2)
        If relativeAngle > Math.PI Then relativeAngle -= CSng(Math.PI * 2)
        '// If deviation is less than field of view, point is visible
        If Math.Abs(relativeAngle) < fov Then Return RelativePointPosition.inside
        '// Calculate the direction the point is relative to the view vector
        If relativeAngle > 0 Then Return RelativePointPosition.clock
        Return RelativePointPosition.anticlock
    End Function

    '// Enum to specify the position of a point for the above checks
    Private Enum RelativePointPosition
        inside = 0
        clock = 1
        anticlock = 2
    End Enum

    '// Return an array of chunks to be displayed
    Public Shared Function Instances(stepSize As Integer, location As COORD3Sng, playerAngle As Single, fov As Single, renderDistance As Integer) As Single()
        Dim lstInstances As New List(Of Single)
        Dim playerChunk As New COORD2Sng()
        Dim showChunk(renderDistance * 2, renderDistance * 2) As Boolean
        Dim testChunk As New COORD2Sng()
        playerChunk.x = location.x / stepSize
        playerChunk.y = location.z / stepSize
        '// Check if chunk is visible and add to list
        For i = 0 To renderDistance * 2 + 1
            For j = 0 To renderDistance * 2 + 1
                testChunk.x = CSng(Math.Floor(playerChunk.x)) - renderDistance + i
                testChunk.y = CSng(Math.Floor(playerChunk.y)) - renderDistance + j
                If ShouldDisplayChunk(testChunk, playerChunk, playerAngle, fov) Then
                    lstInstances.Add(testChunk.x)
                    lstInstances.Add(testChunk.y)
                End If
            Next
        Next
        Return lstInstances.ToArray()
    End Function
End Class

