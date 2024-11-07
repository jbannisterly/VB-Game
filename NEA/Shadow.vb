Option Strict On
Imports NEA.OpenGLImporter
Imports NEA.CoordDataTypes
Imports System.Runtime.InteropServices
'// Class to manage shadow maps
Public Class Shadow
    Private monitorSize As COORD2Short
    Private shadowSizes As Single()
    Private focusPoint As COORD2Sng
    Public lightGradient As Single
    Private VAO As UInteger
    Private context As OpenGLContext

    '// Initialise and set references
    Sub New(inMonitorSize As COORD2Short, inShadowSizes As Single(), inVAO As UInteger, vertices As Single(), indices As Integer(), ByRef inContext As OpenGLContext)
        context = inContext
        monitorSize = inMonitorSize
        shadowSizes = inShadowSizes
        VAO = inVAO
        context.glBindVertexArray(VAO)
        InitialiseBuffers(vertices, indices)
    End Sub

    '// Create and initialise buffers
    Private Sub InitialiseBuffers(vertices As Single(), indices As Integer())
        Dim bufferPtr As IntPtr = Marshal.AllocHGlobal(8)

        glGenBuffers(2, bufferPtr)
        OpenGLWrapper.BufferData(vertices, GL_ARRAY_BUFFER, 0, bufferPtr)
        OpenGLWrapper.BufferData(indices, GL_ELEMENT_ARRAY_BUFFER, 1, bufferPtr)

        glEnableVertexAttribArray(0)
        glEnableVertexAttribArray(1)
        glBindBuffer(GL_ARRAY_BUFFER, CUInt(Marshal.ReadInt32(bufferPtr)))
        glVertexAttribPointer(0, 3, GL_FLOAT, False, 0, 0)
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, CUInt(Marshal.ReadInt32(bufferPtr + 4)))
    End Sub

    '// Render terrain to shadow map
    Public Function ProjectTerrainShadows(shadowTexture As UInt32(), size As Int32, shadowProgram As UInt32, shadowReceiver As UInt32) As COORD2Sng
        context.glUseProgram(shadowProgram)
        context.glBindVertexArray(VAO)
        '// Set uniforms
        glUniform2f(glGetUniformLocationStr(CInt(shadowProgram), "lightGradient"), lightGradient, 0)
        glUniform2f(glGetUniformLocationStr(CInt(shadowProgram), "focusPoint"), focusPoint.x, focusPoint.y)
        glBindFramebuffer(GL_FRAMEBUFFER, shadowReceiver)
        InitialiseTarget()
        glClearDepth(0)
        ProjectShadows(shadowSizes, shadowTexture, CInt(shadowProgram), size, GL_UNSIGNED_INT, True)
        Return focusPoint
    End Function

    '// Set uniforms for the shadow program
    Public Sub InitialiseShadowProgram(shadowProgram As Integer)
        context.glUseProgram(CUInt(shadowProgram))
        glUniform2f(glGetUniformLocationStr(shadowProgram, "lightGradient"), lightGradient, 0)
        glUniform2f(glGetUniformLocationStr(shadowProgram, "focusPoint"), focusPoint.x, focusPoint.y)
    End Sub

    '// Render dynamic models to the shadow map
    Public Function ProjectModelShadows(shadowTexture As UInt32(), shadowProgram As Integer, shadowReceiver As UInt32, mob As Mob, isItem As Boolean, parentMatrix As Matrices(), targetBone As Integer) As COORD2Sng
        Dim modelMatrix As Matrices
        Dim childRootMatrices As Matrices() = Matrices.Copy(mob.transformationMatricesOrigin)

        '// Generate matrices
        modelMatrix = mob.GetModelMatrixBoth(isItem, parentMatrix, targetBone)
        childRootMatrices = mob.GetItemMatrices(modelMatrix)

        context.glBindVertexArray(mob.model.VAOShadow)
        mob.model.GLTFMatrices(mob.animationProgress, shadowProgram, modelMatrix, mob.animationName, CSng(mob.currentAnimationStartTime), mob.animationList.ToArray, mob.transformationMatricesInverse)
        glBindFramebuffer(GL_FRAMEBUFFER, shadowReceiver)
        InitialiseTarget()
        ProjectShadows(shadowSizes, shadowTexture, shadowProgram, CInt(mob.model.numVertices), mob.model.indexType, False)

        '// Recursively draw all shadows of child models
        For i = 0 To mob.children.Count - 1
            ProjectModelShadows(shadowTexture, shadowProgram, shadowReceiver, mob.children(i), True, childRootMatrices, mob.childrenBones(i))
        Next

        Return focusPoint
    End Function

    '// Render shadows to texture using shadow map program
    Private Sub ProjectShadows(shadowSizes As Single(), shadowTexture As UInteger(), shadowProgram As Integer, numVertices As Integer, indexType As UInteger, clear As Boolean)
        '// Iterate through all sizes of shadow map
        '// A LOD approach is used to improve performance
        For i = 0 To shadowSizes.Length - 1
            '// Set target texture and size
            OpenGLWrapper.WriteToTextureInit(context, shadowTexture(i), SHADOWRES, SHADOWRES, GL_DEPTH_ATTACHMENT, GL_DEPTH_COMPONENT, clear)
            context.glBindTexture(GL_TEXTURE_0, shadowTexture(i))
            glUniform1f(glGetUniformLocationStr(shadowProgram, "scale"), 1 / shadowSizes(i))
            '// Clear shadow map if necessary
            If clear Then glClear(256)
            '// Render to shadow map
            glDrawElements(GL_TRIANGLES, numVertices, indexType, 0)
        Next
    End Sub

    '// Set the render target to the size of the texture and render highest object
    Private Sub InitialiseTarget()
        glActiveTexture(GL_TEXTURE_0)
        glDepthFunc(GL_GREATER)
        glViewport(0, 0, SHADOWRES, SHADOWRES)
        glEnable(GL_DEPTH_TEST)
        glDrawBuffer(GL_NONE)
    End Sub

    '// Focus point is the location projected by the light vector
    '// This is the centre of all shadow maps
    Public Function SetFocusPoint(location As COORD3Sng, totT As Single) As COORD2Sng
        focusPoint.x = CSng(location.x - Math.Tan(totT) * location.y)
        focusPoint.y = location.z
        Return focusPoint
    End Function

    '// Create new textures to store shadow maps
    Public Function GenerateShadowTextures(quantity As Integer) As UInt32()
        Dim shadowTextures(quantity - 1) As UInt32
        Dim texturePtr As IntPtr = Marshal.AllocHGlobal(quantity * 4)
        glGenTextures(quantity, texturePtr)
        For i = 0 To quantity - 1
            shadowTextures(i) = CUInt(Marshal.ReadInt32(texturePtr + 4 * i))
            OpenGLWrapper.WriteToTextureInit(context, shadowTextures(i), SHADOWRES, SHADOWRES, GL_DEPTH_ATTACHMENT, GL_DEPTH_COMPONENT, True)
        Next
        Marshal.FreeHGlobal(texturePtr)
        Return shadowTextures
    End Function

    '// Set framebuffer target back to the main screen and reset size
    Public Sub ResetTarget()
        glDrawBuffer(GL_FRONT)
        glViewport(0, 0, monitorSize.x, monitorSize.y)
        glBindFramebuffer(GL_FRAMEBUFFER, 0)
    End Sub

    '// Copy results from base static shadow maps
    '// Allows static objects to be rendered once while dynamic maps are generated every frame
    Public Sub CopyShadows(shadowStatic As UInteger(), shadowDynamic As UInteger())
        For i = 0 To shadowStatic.Length - 1
            glCopyImageSubData(shadowStatic(i), GL_TEXTURE_2D, 0, 0, 0, 0, shadowDynamic(i), GL_TEXTURE_2D, 0, 0, 0, 0, SHADOWRES, SHADOWRES, 1)
        Next
    End Sub
End Class

