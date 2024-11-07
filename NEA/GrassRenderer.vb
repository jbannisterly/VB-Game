Option Strict On
Imports NEA.OpenGLImporter
Imports NEA.CoordDataTypes
'// Handles the rendering of grass
Public Class GrassRenderer
    Public Shared context As OpenGLContext
    Public Shared TextureBindings As UInteger()

    Public Shared Sub RenderGrass(program As UInt32, vao As UInt32, perspectiveMatrix As Matrices, grassTexture As UInt32, chunkCount As Integer, playerPos As COORD3Sng, cameraPos As COORD3Sng, cameraAngle As Single, cameraElevation As Single, rain As Boolean)
        '// Get normalised vector for camera view direction
        Dim cameraView As New COORD3Sng()
        cameraView.x = CSng(Math.Sin(cameraAngle) * Math.Cos(cameraElevation))
        cameraView.y = CSng(-Math.Sin(cameraElevation))
        cameraView.z = CSng(Math.Cos(cameraAngle) * Math.Cos(cameraElevation))
        '// Load program and buffers
        context.glUseProgram(program)
        context.glBindVertexArray(vao)
        '// Write uniform variables to GPU
        glUniformMatrix4fv(glGetUniformLocationStr(CInt(program), "perspectiveMatrix"), 1, False, perspectiveMatrix.ToOpenGLMatrix())
        glUniform3f(glGetUniformLocationStr(CInt(program), "playerPos"), playerPos.x, playerPos.y, playerPos.z)
        glUniform3f(glGetUniformLocationStr(CInt(program), "cameraPos"), cameraPos.x, cameraPos.y, cameraPos.z)
        glUniform3f(glGetUniformLocationStr(CInt(program), "cameraVector"), cameraView.x, cameraView.y, cameraView.z)
        glUniform1f(glGetUniformLocationStr(CInt(program), "inputValue"), CSng(Timer))
        glUniform1i(glGetUniformLocationStr(CInt(program), "rain"), If(rain, 1, 0))
        If TextureBindings(0) = 0 Then
            TextureBindings(0) = grassTexture
        End If
        TextureLoader.BindTextures(TextureBindings)
        '// Draw grass
        glDrawArrays(GL_POINTS, 0, 40000)
    End Sub

    '// Create grass buffers and bind textures
    Public Shared Sub InitialiseGrass(program As UInt32, vao As UInt32, vertexBuffer As UInt32, heightMap As UInt32, normalMap As UInt32)
        context.glUseProgram(program)
        context.glBindVertexArray(vao)

        glEnableVertexAttribArray(0)
        glEnableVertexAttribArray(1)

        '// Create grass buffer
        glBindBuffer(GL_ARRAY_BUFFER, vertexBuffer)
        glVertexAttribPointer(0, 3, GL_FLOAT, False, 0, 0)

        '// Use the dynamically generated heightmap
        context.glBindTexture(GL_TEXTURE_0 + 3, heightMap)
        glUniform1i(glGetUniformLocationStr(CInt(program), "heightmap"), 3)

        context.glBindTexture(GL_TEXTURE_0 + 4, normalMap)
        glUniform1i(glGetUniformLocationStr(CInt(program), "normalmap"), 4)

        '// Bind green grass texture
        glUniform1i(glGetUniformLocationStr(CInt(program), "grassTexture"), 0)

        TextureBindings = {0, 0, 0, heightMap, normalMap}
    End Sub
End Class

