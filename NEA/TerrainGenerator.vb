#Const DEBUG = False
Option Strict On
Imports NEA.OpenGLImporter
Imports NEA.CoordDataTypes
Imports System.Runtime.InteropServices
'// Class to manage generation of heightmaps
Public Class TerrainGenerator
    Public framebufferID As UInteger
    Public terrainProgram As UInteger
    Public textureID As UInteger
    Public normalTextureID As UInteger
    Public noiseTextureID As UInteger
    Public vertexPtr As IntPtr
    Public context As OpenGLContext

    '// Initialisation
    Sub New(noisePtr As IntPtr, ByRef inContext As OpenGLContext)
        Dim texturePtr As IntPtr = Marshal.AllocHGlobal(1024)
        Dim frameBufferPtr As IntPtr = Marshal.AllocHGlobal(1024)

        '// Set reference to OpenGL context
        context = inContext
        terrainProgram = CUInt(OpenGLWrapper.CreateProgram(context, "terrainGenerator", Shaders.VERTEX_SHADER_TERRAIN_GENERATOR, Shaders.FRAGMENT_SHADER_TERRAIN_GENERATOR))

        '// Generate textures
        glGenTextures(3, texturePtr)
        textureID = CUInt(Marshal.ReadInt32(texturePtr))
        normalTextureID = CUInt(Marshal.ReadInt32(texturePtr + 4))
        noiseTextureID = CUInt(Marshal.ReadInt32(texturePtr + 8))
        context.glBindTexture(GL_TEXTURE_0, noiseTextureID)
        '// Noise texture is an input texture so the generator uses the same random numbers
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RG16F, 128, 128, 0, GL_RG, GL_FLOAT, noisePtr)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST)

        '// Initialise framebuffer
        glGenFramebuffers(1, frameBufferPtr)
        framebufferID = CUInt(Marshal.ReadInt32(frameBufferPtr))
        glBindFramebuffer(GL_FRAMEBUFFER, framebufferID)

        '// Initialise heightmap and normal map target textures
        OpenGLWrapper.WriteToTextureInit(context, textureID, 1024, 1024, GL_DEPTH_ATTACHMENT, GL_DEPTH_COMPONENT, True,, GL_DEPTH_COMPONENT32F)
        OpenGLWrapper.WriteToTextureInit(context, normalTextureID, 1024, 1024, GL_COLOR_ATTACHMENT, GL_RGB, True)

        vertexPtr = InitialiseVertices()

        Marshal.FreeHGlobal(texturePtr)
        Marshal.FreeHGlobal(frameBufferPtr)
    End Sub

    '// Generate vertices for a large square
    Private Function InitialiseVertices() As IntPtr
        Dim verticesData As Single() = {-1, -1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, 1, 1, 0, -1, 1, 0}
        Dim vertPtr As IntPtr = Marshal.AllocHGlobal(verticesData.Length * 4)
        Marshal.Copy(verticesData, 0, vertPtr, verticesData.Length)
        Return vertPtr
    End Function

    '// Generate height and normal map using a shader
    Public Sub GenerateTerrain(monitorSize As COORD2Short, offset As COORD2Sng)
        '// Bind buffers, programs and textures
        context.glBindVertexArray(0)
        glBindBuffer(GL_ARRAY_BUFFER, 0)
        context.glUseProgram(terrainProgram)
        glBindFramebuffer(GL_FRAMEBUFFER, framebufferID)

        context.glBindTexture(GL_TEXTURE_0 + 1, noiseTextureID)
        context.glBindTexture(GL_TEXTURE_0, textureID)

        '// Set uniforms
        glUniform1i(glGetUniformLocationStr(CInt(terrainProgram), "noise"), 1)
        glUniform2f(glGetUniformLocationStr(CInt(terrainProgram), "offset"), offset.x, offset.y)

        '// Clear maps and initialise render target
        glClearDepth(-1)
        glClearColor(0, 1, 0, 1)
        glClear(256 + 16384)
        glEnable(GL_TEXTURE_2D)
        glEnable(GL_DEPTH_TEST)
        glDepthFunc(GL_GREATER)
        glViewport(0, 0, 1024, 1024)
        glEnableClientState(GL_VERTEX_ARRAY)
        glVertexPointer(3, GL_FLOAT, 0, vertexPtr)
        '// Create heightmap
        glDrawArrays(GL_TRIANGLES, 0, 6)
        '// Ensure heightmap has finished generating before it is used
        glFlush()
        '// Reset render target
        glDisableClientState(GL_VERTEX_ARRAY)
        glViewport(0, 0, monitorSize.x, monitorSize.y)
        glBindFramebuffer(GL_FRAMEBUFFER, 0)
    End Sub
End Class

