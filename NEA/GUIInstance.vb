Option Strict On
Imports NEA.OpenGLImporter
Imports System.Runtime.InteropServices
Imports System.Console

'// Provides an overall manager class for rendering forms
Public Class GUIInstance
    Public relMouse As New GUIObject.Form.COORD
    Public GUIProgram As UInt32
    Public fontTextureMap As TextureLoader.TextureMap
    Private context As OpenGLContext
    Dim bufferPosition As UInt32
    Dim bufferColour As UInt32
    Dim bufferTexture As UInt32
    Dim VAO As UInt32
    Private numVertices As Integer
    Private windowSize As CoordDataTypes.COORD2Short

    '// Initialise instance
    Sub New(inWindowSize As CoordDataTypes.COORD2Short, inContext As OpenGLContext)
        context = inContext
        GUIProgram = CUInt(OpenGLWrapper.CreateProgram(context, "GUI", Shaders.VERTEX_SHADER_GUI, Shaders.FRAGMENT_SHADER_GUI))

        context.glUseProgram(GUIProgram)
        InitialiseBuffers()
        InitialiseTextures()
        glEnableVertexAttribArray(0)
        glEnableVertexAttribArray(1)
        glEnableVertexAttribArray(2)
        windowSize = inWindowSize
    End Sub

    '// Create buffers to store controls data
    Private Sub InitialiseBuffers()
        Dim bufferPtr As IntPtr = Marshal.AllocHGlobal(12)
        Dim vaoPtr As IntPtr = Marshal.AllocHGlobal(4)

        glGenVertexArrays(1, vaoPtr)
        VAO = CUInt(Marshal.ReadInt32(vaoPtr))

        context.glBindVertexArray(VAO)

        glGenBuffers(3, bufferPtr)
        bufferPosition = CUInt(Marshal.ReadInt32(bufferPtr, 0))
        bufferColour = CUInt(Marshal.ReadInt32(bufferPtr, 4))
        bufferTexture = CUInt(Marshal.ReadInt32(bufferPtr, 8))
        Marshal.FreeHGlobal(bufferPtr)
        Marshal.FreeHGlobal(vaoPtr)
    End Sub

    '// Load font textures
    '// Characters are stored in one large texture (texture map, or atlas)
    '// This prevents many textures from being needed (typically there is a maximum of 16 with OpenGL)
    Private Sub InitialiseTextures()
        Dim texturePtr As IntPtr = Marshal.AllocHGlobal(4)
        Dim fontTextureSize As New CoordDataTypes.COORD2Short

        fontTextureSize.x = 1024
        fontTextureSize.y = 1024
        fontTextureMap = New TextureLoader.TextureMap(fontTextureSize, context)

        glGenTextures(1, texturePtr)
        fontTextureMap.textureID = CUInt(Marshal.ReadInt32(texturePtr))
        fontTextureMap.AddTexturesInDirectory("Resources\Textures\Font")
        fontTextureMap.LoadTexture(fontTextureMap.textureID, GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)

        context.glBindTexture(GL_TEXTURE_0, fontTextureMap.textureID)
        glUniform1i(glGetUniformLocationStr(CInt(GUIProgram), "fontTex"), 0)
        Marshal.FreeHGlobal(texturePtr)
    End Sub

    '// Load buffer data of form
    Public Sub LoadForm(frmToRender As GUIObject.Form, mousePos As GUIObject.Form.COORD, displayMouse As Boolean, topZPass As Boolean)
        '// Forms do not have may vertices, 10000 is ample
        Dim position(10000) As Single
        Dim texture(10000) As Single
        Dim colour(10000) As Single

        numVertices = frmToRender.GenerateVertexData(position, texture, colour, mousePos, fontTextureMap, displayMouse, topZPass)

        '// Copy data to buffer
        OpenGLWrapper.BufferDataGUI(position, bufferPosition, numVertices * 3)
        OpenGLWrapper.BufferDataGUI(texture, bufferTexture, numVertices * 2)
        OpenGLWrapper.BufferDataGUI(colour, bufferColour, numVertices * 3)

        '// Bind buffers to inputs in shader
        glBindBuffer(GL_ARRAY_BUFFER, bufferPosition)
        glVertexAttribPointer(0, 3, GL_FLOAT, False, 0, 0)
        glBindBuffer(GL_ARRAY_BUFFER, bufferTexture)
        glVertexAttribPointer(1, 2, GL_FLOAT, False, 0, 0)
        glBindBuffer(GL_ARRAY_BUFFER, bufferColour)
        glVertexAttribPointer(2, 3, GL_FLOAT, False, 0, 0)
    End Sub

    '// Display form to screen
    Public Sub RenderScreen(toClear As Boolean)
        glClearColor(0, 0, 1, 1)
        glDepthFunc(GL_ALWAYS)
        '// Clear screen if needed (not needed for second pass)
        If toClear Then glClear(GL_COLOUR_BUFFER_BIT)
        context.glUseProgram(GUIProgram)
        glDrawArrays(GL_TRIANGLES, 0, numVertices)
    End Sub

    '// Render a 3D model in the place of a grid view or render target object
    '// Used to display images of items in the inventory
    Public Sub RenderModels(frmCurrent As GUIObject.Form)
        Dim currentGrid As GUIObject.GridView
        For i = 0 To frmCurrent.children.Count - 1
            If frmCurrent.children(i).controlType = GUIObject.Controls.RenderTarget Then
                CType(frmCurrent.children(i), GUIObject.RenderTarget).DisplayModel(New CoordDataTypes.COORD3Sng(1, 0, 0))
            End If
            If frmCurrent.children(i).controlType = GUIObject.Controls.GridView Then
                currentGrid = CType(frmCurrent.children(i), GUIObject.GridView)
                '// Iterate through all items in the inventory grid and display them
                For j = 0 To currentGrid.itemCount - 1
                    currentGrid.items(j).image.DisplayModel(New CoordDataTypes.COORD3Sng(1, 0, 0))
                Next
            End If
        Next
        '// Reset viewport to whole screen
        '// Render targets will have set the viewport to just where the image is
        glViewport(0, 0, windowSize.x, windowSize.y)
    End Sub
End Class

