#Const DEBUG = True
Option Strict On
Imports NEA.OpenGLImporter
Imports System.Runtime.InteropServices
Imports System.Console
'// Provides a wrapper around commonly used OpenGL functions
Public Class OpenGLWrapper
    '// Creates a GLSL program and binds shaders to it
    Public Shared Function CreateProgram(ByRef context As OpenGLContext, descriptor As String, vertexShader As String, fragmentShader As String, Optional geometryShader As String = "") As UInt32
        Dim vs As UInt32
        Dim fs As UInt32
        Dim gs As UInt32
        Dim hasGeometyShader As Boolean = geometryShader <> ""
        Dim program As UInt32

        '// Create vertex shader
        vs = glCreateShader(GL_VERTEX_SHADER)
        glShaderSourceStr(vs, 1, vertexShader)
        glCompileShader(vs)
        '// Create fragment shader
        fs = glCreateShader(GL_FRAGMENT_SHADER)
        glShaderSourceStr(fs, 1, fragmentShader)
        glCompileShader(fs)
        '// Conditionally create geometry shader
        If hasGeometyShader Then
            gs = glCreateShader(GL_GEOMETRY_SHADER)
            glShaderSourceStr(gs, 1, geometryShader)
            glCompileShader(gs)
        End If

        '// Create an error log file if a shader has an error
#If DEBUG Then
        ShaderDebug(vs)
        If hasGeometyShader Then ShaderDebug(gs)
        ShaderDebug(fs)
#End If

        '// Create program and attatch shader
        program = glCreateProgram()
        glAttachShader(program, fs)
        glAttachShader(program, vs)
        If hasGeometyShader Then glAttachShader(program, gs)
        glLinkProgram(program)
        context.glUseProgram(program)

        '// Check shader was linked successfully
#If DEBUG Then
        Dim linked As Integer
        glGetProgramiv(program, GL_LINK_STATUS, linked)
        'Debug.Log(program & " link status " & linked)
#End If

        Return program
    End Function

    '// Write shader debug information to a file
    Private Shared Sub ShaderDebug(shaderID As UInteger)
        Dim compiled As Integer
        glGetShaderiv(shaderID, GL_COMPILE_STATUS, compiled)
        '// If not compiled
        If compiled = 0 Then
            '// Write shader debug information to a pointer
            Dim errorMessage As IntPtr = Marshal.AllocHGlobal(10000)
            Dim errorMessageBytes As Byte()
            Dim errorMessageLen As Integer
            glGetShaderInfoLog(shaderID, 10000, errorMessageLen, errorMessage)
            ReDim errorMessageBytes(errorMessageLen - 1)
            '// Copy debug data to byte array
            Marshal.Copy(errorMessage, errorMessageBytes, 0, errorMessageLen)
            Marshal.FreeHGlobal(errorMessage)
            '// Write data to a debug file
            If Not IO.Directory.Exists("DebugLog") Then IO.Directory.CreateDirectory("DebugLog")
            IO.File.WriteAllBytes("DebugLog\Debug_Shader_" & shaderID & ".txt", errorMessageBytes)
        End If
    End Sub

    '// Copy float data to a buffer
    Public Shared Sub BufferDataGUI(data As Single(), bufferID As UInt32, length As Integer)
        '// Copy data to memory
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(length * 4)
        Marshal.Copy(data, 0, dataPtr, length)
        '// Copy data from memory to buffer
        glBindBuffer(GL_ARRAY_BUFFER, bufferID)
        glBufferData(GL_ARRAY_BUFFER, CUInt(length * 4), dataPtr, GL_DYNAMIC_DRAW)
        '// Free memory
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    '// Get contents of a buffer for debugging and cast to 32-bit float
    Public Shared Function DebugBuffer(bufferID As UInt32) As Single()
        Dim data(230 * 3) As Single
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(230 * 4 * 4)
        glBindBuffer(GL_ARRAY_BUFFER, bufferID)
        glGetBufferSubData(GL_ARRAY_BUFFER, 0, 230 * 3 * 4, dataPtr)
        Marshal.Copy(dataPtr, data, 0, 690)
        Marshal.FreeHGlobal(dataPtr)
        Return data
    End Function

    '// Get contents of a buffer for debugging and cast to 16-bit integer
    Public Shared Function DebugIndexBuffer(bufferID As UInt32) As Short()
        Dim data(128) As Short
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(1024)
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, bufferID)
        glGetBufferSubData(GL_ELEMENT_ARRAY_BUFFER, 0, 1024, dataPtr)
        Marshal.Copy(dataPtr, data, 0, 128)
        Marshal.FreeHGlobal(dataPtr)
        Return data
    End Function

    '// Write data from a pointer to a buffer
    Private Shared Sub BufferData(data As IntPtr, bufferType As UInt32, bufferID As UInt32, length As Int32, Optional dynamicOverride As Boolean = False)
        glBindBuffer(CInt(bufferType), bufferID)
        glBufferData(bufferType, CUInt(length), data, If(dynamicOverride, GL_DYNAMIC_DRAW, GL_STATIC_DRAW))
    End Sub

    '// Overloads to write data from an array to a buffer
    '// First copies the data to memory and uses the pointer to write data to specified buffer
    Public Shared Sub BufferData(data As Single(), bufferType As UInt32, bufferIndex As Integer, buffersPtr As IntPtr, Optional dynamicOverride As Boolean = False)
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length * 4)
        Dim bufferID As UInt32 = CUInt(Marshal.ReadInt32(buffersPtr + bufferIndex * 4))
        Marshal.Copy(data, 0, dataPtr, data.Length)
        BufferData(dataPtr, bufferType, bufferID, data.Length * 4)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    Public Shared Sub BufferData(data As Byte(), bufferType As UInt32, bufferIndex As Integer, buffersPtr As IntPtr)
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length)
        Dim bufferID As UInt32 = CUInt(Marshal.ReadInt32(buffersPtr + bufferIndex * 4))
        Marshal.Copy(data, 0, dataPtr, data.Length)
        BufferData(dataPtr, bufferType, bufferID, data.Length)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    Public Shared Sub BufferData(data As Byte(), bufferType As UInt32, bufferID As UInt32)
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length)
        Marshal.Copy(data, 0, dataPtr, data.Length)
        BufferData(dataPtr, bufferType, bufferID, data.Length)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    Public Shared Sub BufferData(data As Integer(), bufferType As UInt32, bufferID As UInt32, Optional dynamicOverride As Boolean = False)
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length * 4)
        Marshal.Copy(data, 0, dataPtr, data.Length)
        BufferData(dataPtr, bufferType, bufferID, data.Length * 4, dynamicOverride)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    Public Shared Sub BufferData(data As Single(), bufferType As UInt32, bufferID As UInt32, Optional dynamicOverride As Boolean = False)
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length * 4)
        Marshal.Copy(data, 0, dataPtr, data.Length)
        BufferData(dataPtr, bufferType, bufferID, data.Length * 4, dynamicOverride)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    Public Shared Sub BufferData(data As Integer(), bufferType As UInt32, bufferIndex As Integer, buffersPtr As IntPtr, Optional dynamicOverride As Boolean = False)
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(data.Length * 4)
        Dim bufferID As UInt32 = CUInt(Marshal.ReadInt32(buffersPtr + bufferIndex * 4))
        Marshal.Copy(data, 0, dataPtr, data.Length)
        BufferData(dataPtr, bufferType, bufferID, data.Length * 4, dynamicOverride)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    '// Specify which buffer is bound to which shader input
    Public Shared Sub BindBufferToProgramAttributes(buffer As UInt32, size As Int32, type As UInt32, index As UInt32, vertType As VertexType)
        glBindBuffer(GL_ARRAY_BUFFER, buffer)
        If vertType = VertexType.INT Then
            '// Integers require a special function
            glVertexAttribIPointer(index, size, type, 0, 0)
        Else
            glVertexAttribPointer(index, size, type, False, 0, 0)
        End If
    End Sub

    '// Enum used for previous function
    Public Enum VertexType
        INT = 0
        FLOAT = 1
    End Enum

    '// Get information from OpenGL
    '// Used for debugging
    Public Shared Function GetInteger(name As UInteger) As UInteger
        Dim data As UInteger
        glGetIntegerv(name, data)
        Return data
    End Function

    '// Initialise a texture to be written to by a shader
    Public Shared Sub WriteToTextureInit(context As OpenGLContext, textureID As UInt32, width As Int32, height As Int32, attachment As UInt32, component As UInt32, clear As Boolean, Optional offset As UInteger = 0, Optional sizeOverride As Integer = -1)
        Dim bindingError As UInteger
        context.glBindTexture(GL_TEXTURE_0 + offset, textureID)
        glEnable(GL_TEXTURE_2D)
        '// Clear texture data as necessary
        If clear Then glTexImage2D(GL_TEXTURE_2D, 0, If(sizeOverride > -1, sizeOverride, CInt(component)), width, height, 0, component, GL_FLOAT, IntPtr.Zero)
        '// Set default filters and wrappings
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE)
        '// Bind texture to output of framebuffer shader
        glFramebufferTexture2D(GL_FRAMEBUFFER, attachment, GL_TEXTURE_2D, textureID, 0)
        bindingError = glGetError()
    End Sub
End Class

