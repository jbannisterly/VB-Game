Option Strict On
Imports System.Runtime.InteropServices
'// Class to import OpenGL functions and constants
Public Class OpenGLImporter
    '// OpenGL constant definitions
    Public Const defaultErrorMessage As String = "If you see this, something went wrong :("
    Public Const GL_NONE As UInt32 = 0
    Public Const GL_POINTS As UInt32 = 0
    Public Const GL_TRIANGLES As UInt32 = 4
    Public Const GL_QUADS As UInt32 = 7
    Public Const GL_FRONT As UInt32 = 1028
    Public Const GL_BACK As UInt32 = 1029
    Public Const GL_CULL_FACE As UInt32 = 2884
    Public Const GL_MAX_TEXTURE_IMAGE_UNITS As UInt32 = 34930
    Public Const GL_ARRAY_BUFFER As UInt32 = 34962
    Public Const GL_ELEMENT_ARRAY_BUFFER As UInt32 = 34963
    Public Const GL_ELEMENT_ARRAY_BUFFER_BINDING As UInt32 = 34965
    Public Const GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING As UInt32 = 34975
    Public Const GL_VERTEX_ATTRIB_ARRAY_POINTER As UInt32 = 34373
    Public Const GL_FRAMEBUFFER As UInt32 = 36160
    Public Const GL_TEXTURE_BUFFER As UInt32 = 35882
    Public Const GL_TEXTURE_2D As UInt32 = 3553
    Public Const GL_TEXTURE_CUBE_MAP As UInt32 = 34067
    Public Const GL_TEXTURE_CUBE_MAP_POSITIVE_X As UInt32 = 34069
    Public Const GL_STATIC_DRAW As UInt32 = 35044
    Public Const GL_DYNAMIC_DRAW As UInt32 = 35048
    Public Const GL_VERTEX_SHADER As UInt32 = 35633
    Public Const GL_FRAGMENT_SHADER As UInt32 = 35632
    Public Const GL_GEOMETRY_SHADER As UInt32 = 36313
    Public Const GL_BYTE As UInt32 = 5120
    Public Const GL_UNSIGNED_BYTE As UInt32 = 5121
    Public Const GL_UNSIGNED_SHORT As UInt32 = 5123
    Public Const GL_INT As UInt32 = 5124
    Public Const GL_UNSIGNED_INT As UInt32 = 5125
    Public Const GL_FLOAT As UInt32 = 5126
    Public Const GL_VERTEX_ARRAY As UInt32 = 32884
    Public Const GL_DEPTH_TEST As UInt32 = 2929
    Public Const GL_LESS As UInt32 = 513
    Public Const GL_GREATER As UInt32 = 516
    Public Const GL_ALWAYS As UInt32 = 519
    Public Const GL_DEPTH_COMPONENT As UInt32 = 6402
    Public Const GL_RED As UInt32 = 6403
    Public Const GL_RGB As UInt32 = 6407
    Public Const GL_RGBA As UInt32 = 6408
    Public Const GL_RGBA32F As UInt32 = 34836
    Public Const GL_RG As UInt32 = 33319
    Public Const GL_R16F As UInt32 = 33325
    Public Const GL_RG16F As UInt32 = 33327
    Public Const GL_BGR As UInt32 = 32992
    Public Const GL_DEPTH_COMPONENT16 As UInt32 = 33189
    Public Const GL_DEPTH_COMPONENT32 As UInt32 = 33191
    Public Const GL_DEPTH_COMPONENT32F As UInt32 = 36012
    Public Const GL_TEXTURE_MAG_FILTER As UInt32 = 10240
    Public Const GL_TEXTURE_MIN_FILTER As UInt32 = 10241
    Public Const GL_TEXTURE_WRAP_S As UInt32 = 10242
    Public Const GL_TEXTURE_WRAP_T As UInt32 = 10243
    Public Const GL_TEXTURE_WRAP_R As UInt32 = 32882
    Public Const GL_NEAREST As UInt32 = 9728
    Public Const GL_LINEAR As UInt32 = 9729
    Public Const GL_LINEAR_MIPMAP_LINEAR As UInt32 = 9987
    Public Const GL_NEAREST_MIPMAP_NEAREST As UInt32 = 9984
    Public Const GL_TEXTURE_0 As UInt32 = 33984
    Public Const GL_CURRENT_PROGRAM As UInt32 = 35725
    Public Const GL_COMPILE_STATUS As UInt32 = 35713
    Public Const GL_LINK_STATUS As UInt32 = 35714
    Public Const GL_VALIDATE_STATUS As UInt32 = 35715
    Public Const GL_DEPTH_ATTACHMENT As UInt32 = 36096
    Public Const GL_COLOR_ATTACHMENT As UInt32 = 36064
    Public Const GL_CLAMP_TO_EDGE As UInt32 = 33071
    Public Const GL_DEPTH_BUFFER_BIT As UInt32 = 256
    Public Const GL_COLOUR_BUFFER_BIT As UInt32 = 16384
    Public Const GL_FRAMEBUFFER_BINDING As UInt32 = 36006

    '// OpenGL function declarations
    Private Shared functionImporter As New ImportFunctions
    Public Shared glGenBuffers As ImportFunctions.glGenBuffers
    Public Shared glGenFramebuffers As ImportFunctions.glGenFramebuffers
    Public Shared glBindBuffer As ImportFunctions.glBindBuffer
    Public Shared glBindFramebuffer As ImportFunctions.glBindFramebuffer
    Public Shared glBufferData As ImportFunctions.glBufferData
    Public Shared glCreateShader As ImportFunctions.glCreateShader
    Public Shared glGenVertexArrays As ImportFunctions.glGenVertexArrays
    Public Shared glBindVertexArray As ImportFunctions.glBindVertexArray
    Public Shared glEnableVertexAttribArray As ImportFunctions.glEnableVertexAttribArray
    Public Shared glVertexAttribPointer As ImportFunctions.glVertexAttribPointer
    Public Shared glVertexAttribIPointer As ImportFunctions.glVertexAttribIPointer
    Public Shared glShaderSource As ImportFunctions.glShaderSource
    Public Shared glGetShaderSource As ImportFunctions.glGetShaderSource
    Public Shared glGetShaderInfoLog As ImportFunctions.glGetShaderInfoLog
    Public Shared glCompileShader As ImportFunctions.glCompileShader
    Public Shared glCreateProgram As ImportFunctions.glCreateProgram
    Public Shared glAttachShader As ImportFunctions.glAttachShader
    Public Shared glLinkProgram As ImportFunctions.glLinkProgram
    Public Shared glUseProgram As ImportFunctions.glUseProgram
    Public Shared glGetBufferSubData As ImportFunctions.glGetBufferSubData
    Public Shared glGetShaderiv As ImportFunctions.glGetShaderiv
    Public Shared glGetProgramiv As ImportFunctions.glGetProgramiv
    Public Shared glUniform1f As ImportFunctions.glUniform1f
    Public Shared glUniform2f As ImportFunctions.glUniform2f
    Public Shared glUniform3f As ImportFunctions.glUniform3f
    Public Shared glUniform3fv As ImportFunctions.glUniform3fv
    Public Shared glUniform1i As ImportFunctions.glUniform1i
    Public Shared glUniform1iv As ImportFunctions.glUniform1iv
    Public Shared glUniformMatrix4fv As ImportFunctions.glUniformMatrix4fv
    Public Shared glUniformMatrix3fv As ImportFunctions.glUniformMatrix3fv
    Public Shared glGetUniformLocation As ImportFunctions.glGetUniformLocation
    Public Shared glActiveTexture As ImportFunctions.glActiveTexture
    Public Shared glFramebufferTexture As ImportFunctions.glFramebufferTexture
    Public Shared glFramebufferTexture2D As ImportFunctions.glFramebufferTexture2D
    Public Shared glCheckFramebufferStatus As ImportFunctions.glCheckFramebufferStatus
    Public Shared glGenerateMipmap As ImportFunctions.glGenerateMipmap
    Public Shared glGetVertexAttribPointerv As ImportFunctions.glGetVertexAttribPointerv
    Public Shared glGetVertexAttribiv As ImportFunctions.glGetVertexAttribiv
    Public Shared glCopyImageSubData As ImportFunctions.glCopyImageSubData
    Public Shared glDrawElementsInstanced As ImportFunctions.glDrawElementsInstanced
    Public Shared glDrawArraysInstanced As ImportFunctions.glDrawArraysInstanced
    Public Shared glVertexAttribDivisor As ImportFunctions.glVertexAttribDivisor
    Public Shared glDrawBuffers As ImportFunctions.glDrawBuffers

    '// Load functions from pointer using wglGetProcAddress
    Public Shared Sub Initialise()
        OpenGL.Initialise()
        glGenBuffers = functionImporter.GetFunctionGenBuffers("glGenBuffers")
        glGenFramebuffers = functionImporter.GetFunctionGenFramebuffers("glGenFramebuffers")
        glBindBuffer = functionImporter.GetFunctionBindBuffer("glBindBuffer")
        glBindFramebuffer = functionImporter.GetFunctionBindFramebuffer("glBindFramebuffer")
        glBufferData = functionImporter.GetFunctionBufferData("glBufferData")
        glCreateShader = functionImporter.GetFunctionCreateShader("glCreateShader")
        glGenVertexArrays = functionImporter.GetFunctionGenVertexArrays("glGenVertexArrays")
        glBindVertexArray = functionImporter.GetFunctionBindVertexArray("glBindVertexArray")
        glEnableVertexAttribArray = functionImporter.GetFunctionEnableVertexAttribArray("glEnableVertexAttribArray")
        glVertexAttribPointer = functionImporter.GetFunctionVertexAttribPointer("glVertexAttribPointer")
        glVertexAttribIPointer = functionImporter.GetFunctionVertexAttribIPointer("glVertexAttribIPointer")
        glShaderSource = functionImporter.GetFunctionShaderSource("glShaderSource")
        glGetShaderSource = functionImporter.GetFunctionGetShaderSource("glGetShaderSource")
        glGetShaderInfoLog = functionImporter.GetFunctionGetShaderInfoLog("glGetShaderInfoLog")
        glCompileShader = functionImporter.GetFunctionCompileShader("glCompileShader")
        glCreateProgram = functionImporter.GetFunctionCreateProgram("glCreateProgram")
        glAttachShader = functionImporter.GetFunctionAttachShader("glAttachShader")
        glLinkProgram = functionImporter.GetFunctionLinkProgram("glLinkProgram")
        glUseProgram = functionImporter.GetFunctionUseProgram("glUseProgram")
        glGetBufferSubData = functionImporter.GetFunctionGetBufferSubData("glGetBufferSubData")
        glGetShaderiv = functionImporter.GetFunctionGetShaderiv("glGetShaderiv")
        glGetProgramiv = functionImporter.GetFunctionGetProgramiv("glGetProgramiv")
        glUniform1f = functionImporter.GetFunctionUniform1f("glUniform1f")
        glUniform2f = functionImporter.GetFunctionUniform2f("glUniform2f")
        glUniform3f = functionImporter.GetFunctionUniform3f("glUniform3f")
        glUniform3fv = functionImporter.GetFunctionUniform3fv("glUniform3fv")
        glUniform1i = functionImporter.GetFunctionUniform1i("glUniform1i")
        glUniform1iv = functionImporter.GetFunctionUniform1iv("glUniform1iv")
        glUniformMatrix4fv = functionImporter.GetFunctionUniformMatrix4fv("glUniformMatrix4fv")
        glUniformMatrix3fv = functionImporter.GetFunctionUniformMatrix3fv("glUniformMatrix3fv")
        glGetUniformLocation = functionImporter.GetFunctionGetUniformLocation("glGetUniformLocation")
        glActiveTexture = functionImporter.GetFunctionActiveTexture("glActiveTexture")
        glFramebufferTexture = functionImporter.GetFunctionFramebufferTexture("glFramebufferTexture")
        glFramebufferTexture2D = functionImporter.GetFunctionFramebufferTexture2D("glFramebufferTexture2D")
        glCheckFramebufferStatus = functionImporter.GetFunctionCheckFramebufferStatus("glCheckFramebufferStatus")
        glGenerateMipmap = functionImporter.GetFunctionGenerateMipmap("glGenerateMipmap")
        glGetVertexAttribPointerv = functionImporter.GetFunctionGetVertexAttribPointerv("glGetVertexAttribPointerv")
        glGetVertexAttribiv = functionImporter.GetFunctionGetVertexAttribiv("glGetVertexAttribiv")
        glCopyImageSubData = functionImporter.GetFunctionCopyImageSubData("glCopyImageSubData")
        glDrawElementsInstanced = functionImporter.GetFunctionDrawElementsInstanced("glDrawElementsInstanced")
        glDrawArraysInstanced = functionImporter.GetFunctionDrawArraysInstanced("glDrawArraysInstanced")
        glVertexAttribDivisor = functionImporter.GetFunctionVertexAttribDivisor("glVertexAttribDivisor")
        glDrawBuffers = functionImporter.GetFunctionDrawBuffers("glDrawBuffers")
    End Sub

    '// Simplify function calls that require strings to be passed to a function
    '// Automatically convert string to pointer to null-terminated string
    Public Shared Function glGetUniformLocationStr(program As Integer, name As String) As Integer
        Return glGetUniformLocation(program, functionImporter.strToPtr(name))
    End Function

    Public Shared Sub glShaderSourceStr(shader As UInt32, size As Int32, code As String)
        glShaderSource(shader, size, {functionImporter.strToPtr(code)}, {code.Length})
    End Sub

    '// OpenGL32.dll contains OpenGL functions up to version 1.1
    '// These cannot be imported using wglGetProcAddress and must be via DLL import
    <DllImport("opengl32.dll")>
    Public Shared Sub glGenTextures(ByVal size As Int32, ByVal textures As IntPtr)
    End Sub

    <DllImport("opengl32.dll")>
    Public Shared Sub glBindTexture(ByVal target As UInt32, ByVal texture As UInt32)
    End Sub

    <DllImport("opengl32.dll")>
    Public Shared Sub glClearColor(r As Single, g As Single, b As Single, a As Single)
    End Sub

    <DllImport("opengl32.dll")>
    Public Shared Sub glClear(flags As UInt16)
    End Sub

    <DllImport("kernel32.dll")>
    Public Shared Function GetLastError() As UInt32
    End Function

    <DllImport("Opengl32.dll")>
    Public Shared Sub glFlush()
    End Sub

    <DllImport("Opengl32.dll")>
    Public Shared Sub glFinish()
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glBegin(mode As Int32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glEnd()
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glVertexPointer(ByVal size As Int32, ByVal type As UInt32, ByVal stride As UInt32, ByVal data As IntPtr)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glDrawArrays(ByVal mode As UInt32, ByVal first As Int32, ByVal count As Int32)
    End Sub

    <DllImport("Opengl32.dll")>
    Public Shared Function glGetString(name As UInt32) As IntPtr
    End Function

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glEnableClientState(ByVal array As UInt32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glDisableClientState(ByVal array As UInt32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glEnable(ByVal cap As UInt32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glDisable(ByVal cap As UInt32)
    End Sub

    <DllImport("Opengl32.dll")>
    Public Shared Sub glDepthFunc(ByVal func As UInt32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glVertex3f(x As Single, y As Single, z As Single)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glTexImage2D(ByVal target As UInt32, ByVal level As Int32, ByVal internalFormat As Int32, ByVal width As Int32, ByVal height As Int32, ByVal border As UInt32, ByVal format As UInt32, ByVal type As UInt32, ByVal pixels As IntPtr)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glTexParameteri(ByVal target As UInt32, ByVal name As UInt32, ByVal param As Int32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glDrawElements(ByVal mode As UInt32, ByVal count As Int32, ByVal type As UInt32, ByVal start As Int32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glGetIntegerv(ByVal name As UInt32, ByRef data As UInt32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Function glGetError() As UInt32
    End Function

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glDrawBuffer(ByVal mode As UInt32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glGetTexImage(ByVal target As UInt32, ByVal level As Int32, ByVal format As UInt32, ByVal type As UInt32, ByVal pixels As IntPtr)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glClearDepth(ByVal depth As Double)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glReadPixels(ByVal x As Int32, ByVal y As Int32, ByVal width As Int32, ByVal height As Int32, ByVal format As UInt32, ByVal type As UInt32, ByVal pixels As IntPtr)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glViewport(ByVal x As Int32, ByVal y As Int32, ByVal width As Int32, ByVal height As Int32)
    End Sub

    <DllImport("OpenGl32.dll")>
    Public Shared Sub glCullFace(ByVal mode As UInt32)
    End Sub
End Class

