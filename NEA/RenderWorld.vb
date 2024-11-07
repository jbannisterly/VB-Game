#Const GRASS = True

Option Strict On
Imports NEA.CoordDataTypes
Imports NEA.OpenGLImporter
Imports System.Runtime.InteropServices

'// Class to manage drawing the main scene
Public Class RenderWorld
    '// Declaration of textures, programs and buffers
    Public terrainProgram As UInteger
    Public skyProgram As UInteger
    Public GLTFProgram As UInteger
    Public deferredProgramArray As UInteger()
    Public reflectionFramebuffer As UInteger
    Private reflectionTexture As UInteger
    Private reflectionDepth As UInteger
    Private deferredFramebuffer As UInteger
    Private deferredNormal As UInteger
    Private deferredDepth As UInteger
    Private deferredColour As UInteger
    Private deferredReflection As UInteger
    Private deferredVAO As UInteger
    Private deferredWorld As UInteger
    Private rainTexture As UInteger
    Private windowSize As COORD2Short
    Private context As OpenGLContext
    Public graphicsBindings As GameWorld.GraphicsSettings

    '// Initialise renderer
    Sub New(inWindowSize As COORD2Short, progTerrain As UInteger, progSky As UInteger, progGLTF As UInteger, progDefer As UInteger(), ByRef inContext As OpenGLContext)
        Dim framebufferPtr As IntPtr = Marshal.AllocHGlobal(32)
        Dim texturePtr As IntPtr = Marshal.AllocHGlobal(32)
        Dim bufferPtr As IntPtr = Marshal.AllocHGlobal(32)
        Dim vaoPtr As IntPtr = Marshal.AllocHGlobal(32)

        Dim currentProgram As UInt32
        Dim location As Integer

        '// Get references to context and programs
        context = inContext

        terrainProgram = progTerrain
        skyProgram = progSky
        GLTFProgram = progGLTF
        deferredProgramArray = progDefer
        windowSize = inWindowSize

        '// Generate framebuffers and target textures
        glGenFramebuffers(2, framebufferPtr)
        reflectionFramebuffer = CUInt(Marshal.ReadInt32(framebufferPtr))
        deferredFramebuffer = CUInt(Marshal.ReadInt32(framebufferPtr + 4))
        glGenTextures(8, texturePtr)
        reflectionTexture = CUInt(Marshal.ReadInt32(texturePtr))
        reflectionDepth = CUInt(Marshal.ReadInt32(texturePtr + 4))
        deferredNormal = CUInt(Marshal.ReadInt32(texturePtr + 8))
        deferredDepth = CUInt(Marshal.ReadInt32(texturePtr + 12))
        deferredWorld = CUInt(Marshal.ReadInt32(texturePtr + 16))
        deferredColour = CUInt(Marshal.ReadInt32(texturePtr + 20))
        deferredReflection = CUInt(Marshal.ReadInt32(texturePtr + 24))
        rainTexture = CUInt(Marshal.ReadInt32(texturePtr + 28))

        TextureLoader.LoadTexture2D("Rain", rainTexture, GL_LINEAR, GL_LINEAR)

        '// Initialise target textures for reflection framebuffer target
        glBindFramebuffer(GL_FRAMEBUFFER, reflectionFramebuffer)
        glViewport(0, 0, CShort(windowSize.x * REFLECTION_SCALE), CShort(windowSize.y * REFLECTION_SCALE))
        OpenGLWrapper.WriteToTextureInit(context, reflectionTexture, CShort(windowSize.x * REFLECTION_SCALE), CShort(windowSize.y * REFLECTION_SCALE), GL_COLOR_ATTACHMENT, GL_RGB, True)
        OpenGLWrapper.WriteToTextureInit(context, reflectionDepth, CShort(windowSize.x * REFLECTION_SCALE), CShort(windowSize.y * REFLECTION_SCALE), GL_DEPTH_ATTACHMENT, GL_DEPTH_COMPONENT, True)

        '// Initialise target textures for deferred framebuffer target
        glBindFramebuffer(GL_FRAMEBUFFER, deferredFramebuffer)
        glViewport(0, 0, windowSize.x, windowSize.y)
        OpenGLWrapper.WriteToTextureInit(context, deferredNormal, windowSize.x, windowSize.y, GL_COLOR_ATTACHMENT, GL_RGBA, True)
        OpenGLWrapper.WriteToTextureInit(context, deferredWorld, windowSize.x, windowSize.y, GL_COLOR_ATTACHMENT + 1, GL_RGBA, True,, GL_RGBA32F)
        OpenGLWrapper.WriteToTextureInit(context, deferredColour, windowSize.x, windowSize.y, GL_COLOR_ATTACHMENT + 2, GL_RGBA, True,,)
        OpenGLWrapper.WriteToTextureInit(context, deferredReflection, windowSize.x, windowSize.y, GL_COLOR_ATTACHMENT + 3, GL_RGBA, True,,)

        MultiRenderOutput(5)

        OpenGLWrapper.WriteToTextureInit(context, deferredDepth, windowSize.x, windowSize.y, GL_DEPTH_ATTACHMENT, GL_DEPTH_COMPONENT, True)

        '// Bind input textures to deferred programs
        For i = 0 To deferredProgramArray.Length - 1
            context.glUseProgram(deferredProgramArray(i))
            glGetIntegerv(GL_CURRENT_PROGRAM, currentProgram)
            context.glBindTexture(GL_TEXTURE_0 + 7, Sky.outputTexture)
            glUniform1i(glGetUniformLocationStr(CInt(deferredProgramArray(i)), "INsky"), 7)
            context.glBindTexture(GL_TEXTURE_0 + 6, rainTexture)
            glUniform1i(glGetUniformLocationStr(CInt(deferredProgramArray(i)), "INrain"), 6)
            context.glBindTexture(GL_TEXTURE_0 + 11, deferredReflection)
            glUniform1i(glGetUniformLocationStr(CInt(deferredProgramArray(i)), "INreflection"), 11)
            context.glBindTexture(GL_TEXTURE_0 + 12, deferredNormal)
            glUniform1i(glGetUniformLocationStr(CInt(deferredProgramArray(i)), "INnormal"), 12)
            context.glBindTexture(GL_TEXTURE_0 + 13, deferredWorld)
            glUniform1i(glGetUniformLocationStr(CInt(deferredProgramArray(i)), "INworld"), 13)
            context.glBindTexture(GL_TEXTURE_0 + 14, deferredColour)
            glUniform1i(glGetUniformLocationStr(CInt(deferredProgramArray(i)), "INcolour"), 14)
            context.glBindTexture(GL_TEXTURE_0 + 15, reflectionTexture)
            location = glGetUniformLocationStr(CInt(deferredProgramArray(i)), "reflectionTexture")
            glUniform1i(location, 15)
            location = glGetUniformLocationStr(CInt(deferredProgramArray(i)), "inverseScreenCoord")
            glUniform2f(location, CSng(1 / windowSize.x), CSng(1 / windowSize.y))
        Next

        '// Free memory
        Marshal.FreeHGlobal(framebufferPtr)
        Marshal.FreeHGlobal(texturePtr)
        Marshal.FreeHGlobal(bufferPtr)
        Marshal.FreeHGlobal(vaoPtr)
    End Sub

    '// Set a program to render to multiple targets
    Private Sub MultiRenderOutput(count As Integer)
        Dim drawbuffers As IntPtr = Marshal.AllocHGlobal(count * 4)
        Dim attachments(count - 1) As Integer
        '// Get targets
        For i = 0 To count - 1
            attachments(i) = CInt(GL_COLOR_ATTACHMENT + i)
        Next
        Marshal.Copy(attachments, 0, drawbuffers, count)
        '// Assign target attachments
        glDrawBuffers(count, drawbuffers)
        Marshal.FreeHGlobal(drawbuffers)
    End Sub

    '// Rebind textures that are overwritten in other parts of the program
    Private Sub LoadDeferredTextures()
        context.glBindTexture(GL_TEXTURE_0 + 11, deferredReflection)
        context.glBindTexture(GL_TEXTURE_0 + 7, Sky.outputTexture)
    End Sub

    '// Display the scene to the screen
    Public Sub RenderWorld(character As Player, monitorSize As COORD2Short, enemies As EnemyManager, NPCs As NPCManager, lightSource As COORD3Sng, shadowFocusPoint As COORD2Sng, chunkBuffer As UInteger, terrainVAO As UInteger, grassProgram As UInt32, grassVAO As UInt32, grassTexture As UInt32, totT As Single, timeOfDay As Single, reflection As Boolean, ripples As RippleController, saturation As Single, renderMobs As Boolean, rain As Boolean, rainProgram As UInt32, cameraPosition As COORD3Sng, cameraRotation As Single, cameraElevation As Single, renderPlayer As Boolean)
        Dim matrixView As New Matrices(4, 4, True)
        Dim matrixRelative As New Matrices(4, 4, True)
        Dim matrixPerspective As New Matrices(4, 4, True)
        Dim matrixModel As New Matrices(4, 4, True)
        Dim matrixNormal As New Matrices(4, 4, True)
        Dim numChunks As Integer
        Dim deferredProgram As UInteger = deferredProgramArray(graphicsBindings.GetGraphicsSettings(GameWorld.GraphicsSettings.SettingsIndex.Shadow))
        Dim vertices As IntPtr = Sky.vertices
        Dim characterSpeed As Single = CSng(Math.Atan2(Math.Sqrt(character.deltaPosition.x * character.deltaPosition.x + character.deltaPosition.z * character.deltaPosition.z), 10))
        Dim rainMatrix As Matrices = Sky.GetSkyMatrix(windowSize, cameraElevation - characterSpeed, cameraRotation, reflection)
        context.glUseProgram(deferredProgram)

        '// Change render target depending on whether a reflection is being rendered
        If reflection Then
            glBindFramebuffer(GL_FRAMEBUFFER, reflectionFramebuffer)
            glViewport(0, 0, CShort(windowSize.x * REFLECTION_SCALE), CShort(windowSize.y * REFLECTION_SCALE))
        Else
            glBindFramebuffer(GL_FRAMEBUFFER, 0)
            glViewport(0, 0, windowSize.x, windowSize.y)
        End If

        '// Get matrices to transform from world space to screen space
        MatrixGenerator.GeneratePlayerMatrices(matrixRelative, matrixView, matrixPerspective, monitorSize, character, reflection, cameraPosition, cameraRotation, cameraElevation)

        glClearDepth(1)
        glDepthFunc(GL_ALWAYS)

        '// Render sky to screen
        Sky.DisplaySky(cameraElevation, cameraRotation, monitorSize, skyProgram, CUInt(Marshal.ReadInt32(Sky.texPtr)), {CSng(Math.Sin(timeOfDay)), CSng(Math.Cos(timeOfDay)), 0}, timeOfDay, reflection, saturation, rain)

        '// Set target to deferred framebuffer and clear output
        glBindFramebuffer(GL_FRAMEBUFFER, deferredFramebuffer)
        glClearDepth(1)
        glClearColor(1, 1, 1, 1)
        glClear(GL_COLOUR_BUFFER_BIT + 256)
        glDepthFunc(GL_LESS)

        '// Render terrain
        context.glUseProgram(terrainProgram)
        context.glBindVertexArray(terrainVAO)
        '// Do not render terrain facing backwards such as behind a hill
        glEnable(GL_CULL_FACE)
        glCullFace(If(reflection, GL_BACK, GL_FRONT))
        '// Render terrain
        TerrainRenderer.LoadMatrices(CInt(terrainProgram), CInt(deferredProgram), matrixPerspective, matrixView, matrixRelative, lightSource, shadowFocusPoint, character.location, cameraPosition, reflection)
        numChunks = TerrainRenderer.DisplayTerrain(cameraPosition, chunkBuffer, cameraRotation)

        '// Render mobs
        context.glUseProgram(GLTFProgram)
        '// Set uniform data
        glUniform3f(glGetUniformLocationStr(CInt(GLTFProgram), "lightSource"), lightSource.x, lightSource.y, lightSource.z)
        glUniform1i(glGetUniformLocationStr(CInt(GLTFProgram), "reflection"), If(reflection, 1, 0))
        glUniform3f(glGetUniformLocationStr(CInt(GLTFProgram), "playerPos"), cameraPosition.x, cameraPosition.y, cameraPosition.z)
        '// Render all mobs
        If renderMobs Then
            enemies.RenderEnemies(GLTFProgram, matrixRelative, matrixView, matrixPerspective)
            NPCs.RenderNPCs(GLTFProgram, matrixRelative, matrixView, matrixPerspective)
            If renderPlayer Then
                character.Display(GLTFProgram, matrixRelative, matrixView, matrixPerspective, False, {New Matrices(4, 4, True)}, -1)
            End If
            character.TryDisplayWeaponIcon(GLTFProgram, monitorSize)
        End If

        '// Render grass without culling as all should be shown
        glDisable(GL_CULL_FACE)
#If GRASS Then
        GrassRenderer.RenderGrass(grassProgram, grassVAO, matrixPerspective, grassTexture, numChunks, character.location, cameraPosition, cameraRotation, cameraElevation, rain)
#End If

        '// Set output target to either the reflection framebuffer or the screen
        If reflection Then
            glBindFramebuffer(GL_FRAMEBUFFER, reflectionFramebuffer)
        Else
            glBindFramebuffer(GL_FRAMEBUFFER, 0)
        End If

        '// Load uniforms and vertices
        TerrainRenderer.LoadUniformsDeferred(CInt(deferredProgram), lightSource, shadowFocusPoint, character.location, cameraPosition, cameraRotation, totT, timeOfDay, reflection, ripples.GetRippleData(), saturation)
        glDepthFunc(GL_ALWAYS)
        context.glBindVertexArray(0)
        glBindBuffer(GL_ARRAY_BUFFER, 0)
        glEnableClientState(GL_VERTEX_ARRAY)
        glUniform3f(glGetUniformLocationStr(CInt(deferredProgram), "light"), lightSource.x, lightSource.y, lightSource.z)
        glUniform1i(glGetUniformLocationStr(CInt(deferredProgram), "sky"), 5)
        glUniform1f(glGetUniformLocationStr(CInt(deferredProgram), "oofFactor"), GetOof(CSng(Timer) - character.lastOof))
        glUniform1i(glGetUniformLocationStr(CInt(deferredProgram), "rain"), If(rain, 1, 0))
        glUniform1f(glGetUniformLocationStr(CInt(deferredProgram), "time"), CSng(Timer))
        glUniformMatrix4fv(glGetUniformLocationStr(CInt(deferredProgram), "cameraMatrix"), 1, False, rainMatrix.ToOpenGLMatrix)
        '// Load textures - result of deferred render
        LoadDeferredTextures()
        glVertexPointer(3, GL_FLOAT, 0, vertices)
        '// Draw a square to represent the screen
        glDrawArrays(GL_TRIANGLES, 0, 6)

        glDepthFunc(GL_LESS)
    End Sub

    '// Get intensity of red around the screen when hit
    Private Function GetOof(deltaT As Single) As Single
        Const OOF_TIME As Single = 0.2
        Const OOF_INTENSITY As Single = 1.5

        '// Clamp to 0
        If deltaT > OOF_TIME Then Return 0
        '// Increase intensity up to halfway point
        If deltaT < OOF_TIME / 2 Then Return deltaT / OOF_TIME * 2 * OOF_INTENSITY
        '// Decrease intensity beyond halfway point for fade out
        Return (OOF_TIME - deltaT) / OOF_TIME * 2 * OOF_INTENSITY
    End Function
End Class

