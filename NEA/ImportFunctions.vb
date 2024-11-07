Option Strict On
Imports System.Runtime.InteropServices
Imports System.Console

'// Imports OpenGL functions from an address
'// Imports the functions not included in OpenGL 1.1
Public Class ImportFunctions
    Const defaultErrorMessage As String = "If you see this, something went wrong :("

    '// Declaration of delegate subroutines
    Delegate Sub glGenBuffers(ByVal size As Int32, ByVal buffers As IntPtr)
    Delegate Sub glGenFramebuffers(ByVal size As Int32, ByVal buffers As IntPtr)
    Delegate Sub glBindBuffer(ByVal target As Int32, ByVal buffer As UInt32)
    Delegate Sub glBindFramebuffer(ByVal target As Int32, ByVal buffer As UInt32)
    Delegate Sub glBufferData(ByVal target As UInt32, ByVal size As UInt32, ByVal data As IntPtr, ByVal usage As UInt32)
    Delegate Function glCreateShader(ByVal shaderType As UInt32) As UInt32
    Delegate Sub glGenVertexArrays(ByVal size As UInt32, ByVal arrays As IntPtr)
    Delegate Sub glBindVertexArray(ByVal array As UInt32)
    Delegate Sub glEnableVertexAttribArray(ByVal index As UInt32)
    Delegate Sub glVertexAttribPointer(ByVal index As UInt32, ByVal size As Int32, ByVal type As UInt32, ByVal normalised As Boolean, ByVal stride As UInt32, ByVal pointer As UInt32)
    Delegate Sub glVertexAttribIPointer(ByVal index As UInt32, ByVal size As Int32, ByVal type As UInt32, ByVal stride As UInt32, ByVal pointer As UInt32)
    Delegate Sub glShaderSource(ByVal shader As UInt32, ByVal size As Int32, ByVal sourceCode As IntPtr(), ByVal length As Int32())
    Delegate Sub glGetShaderSource(ByVal shader As UInt32, ByVal sizeBuffer As Int32, ByRef sizeReturned As Int32, ByVal source As IntPtr)
    Delegate Sub glGetShaderInfoLog(ByVal shader As UInt32, ByVal sizeBuffer As Int32, ByRef sizeReturned As Int32, ByVal target As IntPtr)
    Delegate Sub glCompileShader(ByVal shader As UInt32)
    Delegate Function glCreateProgram() As UInt32
    Delegate Sub glAttachShader(ByVal program As UInt32, ByVal shader As UInt32)
    Delegate Sub glLinkProgram(ByVal program As UInt32)
    Delegate Sub glUseProgram(ByVal program As UInt32)
    Delegate Function glGetString(ByVal name As UInt32) As IntPtr
    Delegate Sub glGetBufferSubData(ByVal target As UInt32, ByVal offset As UInt32, ByVal size As UInt32, ByVal data As IntPtr)
    Delegate Sub glGetShaderiv(ByVal shader As UInt32, ByVal paramName As UInt32, ByRef param As Int32)
    Delegate Sub glGetProgramiv(ByVal program As UInt32, ByVal paramName As UInt32, ByRef param As Int32)
    Delegate Sub glUniform1f(ByVal location As Int32, ByVal v0 As Single)
    Delegate Sub glUniform2f(ByVal location As Int32, ByVal v0 As Single, ByVal v1 As Single)
    Delegate Sub glUniform3f(ByVal location As Int32, ByVal v0 As Single, ByVal v1 As Single, ByVal v2 As Single)
    Delegate Sub glUniform3fv(ByVal location As Int32, ByVal count As Int32, ByVal v0 As IntPtr)
    Delegate Sub glUniform1i(ByVal location As Int32, ByVal v0 As Int32)
    Delegate Sub glUniform1iv(ByVal location As Int32, ByVal count As Int32, ByVal v0 As Int32)
    Delegate Function glGetUniformLocation(ByVal program As Int32, ByVal name As IntPtr) As Int32
    Delegate Sub glUniformMatrix4fv(ByVal location As Int32, ByVal count As Int32, ByVal transpose As Boolean, ByVal dataPtr As IntPtr)
    Delegate Sub glUniformMatrix3fv(ByVal location As Int32, ByVal count As Int32, ByVal transpose As Boolean, ByVal dataPtr As IntPtr)
    Delegate Sub glActiveTexture(ByVal name As UInt32)
    Delegate Sub glFramebufferTexture(ByVal target As UInt32, ByVal attachment As UInt32, ByVal texture As UInt32, ByVal level As Int32)
    Delegate Sub glFramebufferTexture2D(ByVal target As UInt32, ByVal attachment As UInt32, ByVal texTarget As UInt32, ByVal texture As UInt32, ByVal level As Int32)
    Delegate Function glCheckFramebufferStatus(ByVal target As UInt32) As UInt32
    Delegate Sub glGenerateMipmap(ByVal target As UInt32)
    Delegate Sub glGetVertexAttribPointerv(ByVal index As UInt32, ByVal pname As UInt32, ByRef pointer As UInt32)
    Delegate Sub glGetVertexAttribiv(ByVal index As UInt32, ByVal pname As UInt32, ByRef params As Int32)
    Delegate Sub glCopyImageSubData(ByVal srcName As UInt32, ByVal srcTarget As UInt32, ByVal srcLevel As Int32, ByVal srcX As Int32, ByVal srcY As Int32, ByVal srcZ As Int32, ByVal dstName As UInt32, ByVal dstTarget As UInt32, ByVal dstLevel As Int32, ByVal dstX As Int32, ByVal dstY As Int32, ByVal dstZ As Int32, ByVal srcWidth As Int32, ByVal srcHeight As Int32, ByVal srcDepth As Int32)
    Delegate Sub glDrawElementsInstanced(ByVal mode As UInt32, ByVal count As Int32, ByVal type As UInt32, ByVal indices As Int32, ByVal instancecount As Int32)
    Delegate Sub glDrawArraysInstanced(ByVal mode As UInt32, ByVal first As Int32, ByVal count As UInt32, ByVal instancecount As Int32)
    Delegate Sub glVertexAttribDivisor(ByVal index As UInt32, ByVal divisor As UInt32)
    Delegate Sub glDrawBuffers(ByVal count As Int32, ByVal bufs As IntPtr)

    Sub ErrorMessage()
        WriteLine(defaultErrorMessage)
    End Sub

    '// Default subroutines displaying an error message if the subroutine cannot be loaded
    Sub glGenBuffersPlaceholder(ByVal size As Int32, ByVal buffers As IntPtr)
        ErrorMessage()
    End Sub
    Sub glGenFramebuffersPlaceholder(ByVal size As Int32, ByVal buffers As IntPtr)
        ErrorMessage()
    End Sub
    Sub glBindBufferPlaceholder(ByVal target As Int32, ByVal buffer As UInt32)
        ErrorMessage()
    End Sub
    Sub glBindFramebufferPlaceholder(ByVal target As Int32, ByVal buffer As UInt32)
        ErrorMessage()
    End Sub
    Sub glBufferDataPlaceholder(ByVal target As UInt32, ByVal size As UInt32, ByVal data As IntPtr, ByVal usage As UInt32)
        ErrorMessage()
    End Sub
    Sub glGenVertexArraysPlaceholder(ByVal size As UInt32, ByVal arrays As IntPtr)
        ErrorMessage()
    End Sub
    Function glCreateShaderPlaceholder(ByVal shaderType As UInt32) As UInt32
        ErrorMessage()
        Return 0
    End Function
    Sub glBindVertexArrayPlaceholder(ByVal array As UInt32)
        ErrorMessage()
    End Sub
    Sub glEnableVertexAttribArrayPlaceholder(ByVal index As UInt32)
        ErrorMessage()
    End Sub
    Sub glVertexAttribPointerPlaceholder(ByVal index As UInt32, ByVal size As Int32, ByVal type As UInt32, ByVal normalised As Boolean, ByVal stride As UInt32, ByVal pointer As UInt32)
        ErrorMessage()
    End Sub
    Sub glVertexAttribIPointerPlaceholder(ByVal index As UInt32, ByVal size As Int32, ByVal type As UInt32, ByVal stride As UInt32, ByVal pointer As UInt32)
        ErrorMessage()
    End Sub
    Sub glShaderSourcePlaceholder(ByVal shader As UInt32, ByVal count As Int32, ByVal sourceCode As IntPtr(), ByVal length As Int32())
        ErrorMessage()
    End Sub
    Sub glGetShaderSourcePlaceholder(ByVal shader As UInt32, ByVal sizeBuffer As Int32, ByRef sizeReturned As Int32, ByVal source As IntPtr)
        ErrorMessage()
    End Sub
    Sub glGetShaderInfoLogPlaceholder(ByVal shader As UInt32, ByVal sizeBuffer As Int32, ByRef sizeReturned As Int32, ByVal target As IntPtr)
        ErrorMessage()
    End Sub
    Sub glCompileShaderPlaceholder(ByVal shader As UInt32)
        ErrorMessage()
    End Sub
    Function glCreateProgramPlaceholder() As UInt32
        ErrorMessage()
        Return 0
    End Function
    Sub glAttachShaderPlaceholder(ByVal program As UInt32, ByVal shader As UInt32)
        ErrorMessage()
    End Sub
    Sub glLinkProgramPlaceholder(ByVal program As UInt32)
        ErrorMessage()
    End Sub
    Sub glUseProgramPlaceholder(ByVal program As UInt32)
        ErrorMessage()
    End Sub
    Function glGetStringPlaceholder(ByVal name As UInt32) As IntPtr
        ErrorMessage()
        Return IntPtr.Zero
    End Function
    Sub glGetBufferSubDataPlaceholder(ByVal target As UInt32, ByVal offset As UInt32, ByVal size As UInt32, ByVal data As IntPtr)
        ErrorMessage()
    End Sub
    Sub glGetShaderivPlaceholder(ByVal shader As UInt32, ByVal paramName As UInt32, ByRef param As Int32)
        ErrorMessage()
    End Sub
    Sub glGetProgramivPlaceholder(ByVal shader As UInt32, ByVal paramName As UInt32, ByRef param As Int32)
        ErrorMessage()
    End Sub
    Sub glUniform1fPlaceholder(ByVal location As Int32, ByVal v0 As Single)
        ErrorMessage()
    End Sub
    Sub glUniform2fPlaceholder(ByVal location As Int32, ByVal v0 As Single, ByVal v1 As Single)
        ErrorMessage()
    End Sub
    Sub glUniform3fPlaceholder(ByVal location As Int32, ByVal v0 As Single, ByVal v1 As Single, ByVal v2 As Single)
        ErrorMessage()
    End Sub
    Sub glUniform1iPlaceholder(ByVal location As Int32, ByVal v0 As Int32)
        ErrorMessage()
    End Sub
    Sub glUniform1ivPlaceholder(ByVal location As Int32, ByVal count As Int32, ByVal v0 As Int32)
        ErrorMessage()
    End Sub
    Function glGetUniformLocationPlaceholder(ByVal program As Int32, ByVal name As IntPtr) As Int32
        ErrorMessage()
        Return 0
    End Function
    Sub glUniformMatrix4fvPlaceholder(ByVal location As Int32, ByVal count As Int32, ByVal transpose As Boolean, ByVal dataPtr As IntPtr)
        ErrorMessage()
    End Sub
    Sub glUniformMatrix3fvPlaceholder(ByVal location As Int32, ByVal count As Int32, ByVal transpose As Boolean, ByVal dataPtr As IntPtr)
        ErrorMessage()
    End Sub
    Sub glActiveTexturePlaceholder(ByVal name As UInt32)
        ErrorMessage()
    End Sub
    Sub glFramebufferTexturePlaceholder(ByVal target As UInt32, ByVal attachment As UInt32, ByVal texture As UInt32, ByVal level As Int32)
        ErrorMessage()
    End Sub
    Function glCheckFramebufferStatusPlaceholder(ByVal target As UInt32) As UInt32
        ErrorMessage()
        Return 0
    End Function
    Sub glGetTexImagePlaceholder(ByVal target As UInt32, ByVal level As Int32, ByVal format As UInt32, ByVal type As UInt32, ByVal pixels As IntPtr)
        ErrorMessage()
    End Sub
    Sub glFramebufferTexture2DPlaceholder(ByVal target As UInt32, ByVal attachment As UInt32, ByVal texTarget As UInt32, ByVal texture As UInt32, ByVal level As Int32)
        ErrorMessage()
    End Sub
    Sub glGenerateMipmapPlaceholder(ByVal target As UInt32)
        ErrorMessage()
    End Sub
    Sub glGetVertexAttribPointervPlaceholder(ByVal index As UInt32, ByVal pname As UInt32, ByRef pointer As UInt32)
        ErrorMessage()
    End Sub
    Sub glGetVertexAttribivPlaceholder(ByVal index As UInt32, ByVal pname As UInt32, ByRef params As Int32)
        ErrorMessage()
    End Sub
    Sub glCopyImageSubDataPlaceholder(ByVal srcName As UInt32, ByVal srcTarget As UInt32, ByVal srcLevel As Int32, ByVal srcX As Int32, ByVal srcY As Int32, ByVal srcZ As Int32, ByVal dstName As UInt32, ByVal dstTarget As UInt32, ByVal dstLevel As Int32, ByVal dstX As Int32, ByVal dstY As Int32, ByVal dstZ As Int32, ByVal srcWidth As Int32, ByVal srcHeight As Int32, ByVal srcDepth As Int32)
        ErrorMessage()
    End Sub
    Sub glDrawElementsInstancedPlaceholder(ByVal mode As UInt32, ByVal count As Int32, ByVal type As UInt32, ByVal indices As Int32, ByVal instancecount As Int32)
        ErrorMessage()
    End Sub
    Sub glDrawArraysInstancedPlaceholder(ByVal mode As UInt32, ByVal first As Int32, ByVal count As UInt32, ByVal instancecount As Int32)
        ErrorMessage()
    End Sub
    Sub glVertexAttribDivisorPlaceholder(index As UInt32, divisor As UInt32)
        ErrorMessage()
    End Sub
    Sub glDrawBuffersPlaceholder(ByVal count As Int32, ByVal bufs As IntPtr)
        ErrorMessage()
    End Sub
    Sub glUniform3fvPlaceholder(ByVal location As Int32, ByVal count As Int32, ByVal v0 As IntPtr)
        ErrorMessage()
    End Sub

    '// Functions to link to OpenGL functions
    '// These must be defined separately as each one returns a different type of delegate function
    Function GetFunctionGenBuffers(name As String) As glGenBuffers
        Dim functionDel As glGenBuffers = AddressOf glGenBuffersPlaceholder
        Dim address As IntPtr
        '// Call OpenGL function to get address of subroutine desired
        address = wglGetProcAddress(strToPtr(name))
        '// Cast the address to the subroutine
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGenBuffers)
        Return functionDel
    End Function
    Function GetFunctionGenFramebuffers(name As String) As glGenFramebuffers
        Dim functionDel As glGenFramebuffers = AddressOf glGenFramebuffersPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGenFramebuffers)
        Return functionDel
    End Function
    Function GetFunctionBindBuffer(name As String) As glBindBuffer
        Dim functionDel As glBindBuffer = AddressOf glBindBufferPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glBindBuffer)
        Return functionDel
    End Function
    Function GetFunctionBindFramebuffer(name As String) As glBindFramebuffer
        Dim functionDel As glBindFramebuffer = AddressOf glBindFramebufferPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glBindFramebuffer)
        Return functionDel
    End Function
    Function GetFunctionBufferData(name As String) As glBufferData
        Dim functionDel As glBufferData = AddressOf glBufferDataPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glBufferData)
        Return functionDel
    End Function
    Function GetFunctionCreateShader(name As String) As glCreateShader
        Dim functionDel As glCreateShader = AddressOf glCreateShaderPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glCreateShader)
        Return functionDel
    End Function
    Function GetFunctionGenVertexArrays(name As String) As glGenVertexArrays
        Dim functionDel As glGenVertexArrays = AddressOf glGenVertexArraysPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGenVertexArrays)
        Return functionDel
    End Function
    Function GetFunctionBindVertexArray(name As String) As glBindVertexArray
        Dim functionDel As glBindVertexArray = AddressOf glBindVertexArrayPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glBindVertexArray)
        Return functionDel
    End Function
    Function GetFunctionEnableVertexAttribArray(name As String) As glEnableVertexAttribArray
        Dim functionDel As glEnableVertexAttribArray = AddressOf glEnableVertexAttribArrayPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glEnableVertexAttribArray)
        Return functionDel
    End Function
    Function GetFunctionVertexAttribPointer(name As String) As glVertexAttribPointer
        Dim functionDel As glVertexAttribPointer = AddressOf glVertexAttribPointerPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glVertexAttribPointer)
        Return functionDel
    End Function
    Function GetFunctionVertexAttribIPointer(name As String) As glVertexAttribIPointer
        Dim functionDel As glVertexAttribIPointer = AddressOf glVertexAttribIPointerPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glVertexAttribIPointer)
        Return functionDel
    End Function
    Function GetFunctionShaderSource(name As String) As glShaderSource
        Dim functionDel As glShaderSource = AddressOf glShaderSourcePlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glShaderSource)
        Return functionDel
    End Function
    Function GetFunctionGetShaderSource(name As String) As glGetShaderSource
        Dim functionDel As glGetShaderSource = AddressOf glGetShaderSourcePlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetShaderSource)
        Return functionDel
    End Function
    Function GetFunctionGetShaderInfoLog(name As String) As glGetShaderInfoLog
        Dim functionDel As glGetShaderInfoLog = AddressOf glGetShaderInfoLogPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetShaderInfoLog)
        Return functionDel
    End Function
    Function GetFunctionCompileShader(name As String) As glCompileShader
        Dim functionDel As glCompileShader = AddressOf glCompileShaderPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glCompileShader)
        Return functionDel
    End Function
    Function GetFunctionCreateProgram(name As String) As glCreateProgram
        Dim functionDel As glCreateProgram = AddressOf glCreateProgramPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glCreateProgram)
        Return functionDel
    End Function
    Function GetFunctionAttachShader(name As String) As glAttachShader
        Dim functionDel As glAttachShader = AddressOf glAttachShaderPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glAttachShader)
        Return functionDel
    End Function
    Function GetFunctionLinkProgram(name As String) As glLinkProgram
        Dim functionDel As glLinkProgram = AddressOf glLinkProgramPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glLinkProgram)
        Return functionDel
    End Function
    Function GetFunctionUseProgram(name As String) As glUseProgram
        Dim functionDel As glUseProgram = AddressOf glUseProgramPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUseProgram)
        Return functionDel
    End Function
    Function GetFunctionGetString(name As String) As glGetString
        Dim functionDel As glGetString = AddressOf glGetStringPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetString)
        Return functionDel
    End Function
    Function GetFunctionGetBufferSubData(name As String) As glGetBufferSubData
        Dim functionDel As glGetBufferSubData = AddressOf glGetBufferSubDataPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetBufferSubData)
        Return functionDel
    End Function
    Function GetFunctionGetShaderiv(name As String) As glGetShaderiv
        Dim functionDel As glGetShaderiv = AddressOf glGetShaderivPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetShaderiv)
        Return functionDel
    End Function
    Function GetFunctionGetProgramiv(name As String) As glGetProgramiv
        Dim functionDel As glGetProgramiv = AddressOf glGetProgramivPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetProgramiv)
        Return functionDel
    End Function
    Function GetFunctionUniform1f(name As String) As glUniform1f
        Dim functionDel As glUniform1f = AddressOf glUniform1fPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniform1f)
        Return functionDel
    End Function
    Function GetFunctionUniform2f(name As String) As glUniform2f
        Dim functionDel As glUniform2f = AddressOf glUniform2fPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniform2f)
        Return functionDel
    End Function
    Function GetFunctionUniform3f(name As String) As glUniform3f
        Dim functionDel As glUniform3f = AddressOf glUniform3fPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniform3f)
        Return functionDel
    End Function
    Function GetFunctionUniform3fv(name As String) As glUniform3fv
        Dim functionDel As glUniform3fv = AddressOf glUniform3fvPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniform3fv)
        Return functionDel
    End Function
    Function GetFunctionUniform1i(name As String) As glUniform1i
        Dim functionDel As glUniform1i = AddressOf glUniform1iPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniform1i)
        Return functionDel
    End Function
    Function GetFunctionUniform1iv(name As String) As glUniform1iv
        Dim functionDel As glUniform1iv = AddressOf glUniform1ivPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniform1iv)
        Return functionDel
    End Function
    Function GetFunctionGetUniformLocation(name As String) As glGetUniformLocation
        Dim functionDel As glGetUniformLocation = AddressOf glGetUniformLocationPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetUniformLocation)
        Return functionDel
    End Function
    Function GetFunctionUniformMatrix4fv(name As String) As glUniformMatrix4fv
        Dim functionDel As glUniformMatrix4fv = AddressOf glUniformMatrix4fvPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniformMatrix4fv)
        Return functionDel
    End Function
    Function GetFunctionUniformMatrix3fv(name As String) As glUniformMatrix3fv
        Dim functionDel As glUniformMatrix3fv = AddressOf glUniformMatrix3fvPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glUniformMatrix3fv)
        Return functionDel
    End Function
    Function GetFunctionActiveTexture(name As String) As glActiveTexture
        Dim functionDel As glActiveTexture = AddressOf glActiveTexturePlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glActiveTexture)
        Return functionDel
    End Function
    Function GetFunctionFramebufferTexture(name As String) As glFramebufferTexture
        Dim functionDel As glFramebufferTexture = AddressOf glFramebufferTexturePlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glFramebufferTexture)
        Return functionDel
    End Function
    Function GetFunctionFramebufferTexture2D(name As String) As glFramebufferTexture2D
        Dim functionDel As glFramebufferTexture2D = AddressOf glFramebufferTexture2DPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glFramebufferTexture2D)
        Return functionDel
    End Function
    Function GetFunctionCheckFramebufferStatus(name As String) As glCheckFramebufferStatus
        Dim functionDel As glCheckFramebufferStatus = AddressOf glCheckFramebufferStatusPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glCheckFramebufferStatus)
        Return functionDel
    End Function
    Function GetFunctionGenerateMipmap(name As String) As glGenerateMipmap
        Dim functionDel As glGenerateMipmap = AddressOf glGenerateMipmapPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGenerateMipmap)
        Return functionDel
    End Function
    Function GetFunctionGetVertexAttribPointerv(name As String) As glGetVertexAttribPointerv
        Dim functionDel As glGetVertexAttribPointerv = AddressOf glGetVertexAttribPointervPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetVertexAttribPointerv)
        Return functionDel
    End Function
    Function GetFunctionGetVertexAttribiv(name As String) As glGetVertexAttribiv
        Dim functionDel As glGetVertexAttribiv = AddressOf glGetVertexAttribivPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glGetVertexAttribiv)
        Return functionDel
    End Function
    Function GetFunctionCopyImageSubData(name As String) As glCopyImageSubData
        Dim functionDel As glCopyImageSubData = AddressOf glCopyImageSubDataPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glCopyImageSubData)
        Return functionDel
    End Function
    Function GetFunctionDrawElementsInstanced(name As String) As glDrawElementsInstanced
        Dim functionDel As glDrawElementsInstanced = AddressOf glDrawElementsInstancedPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glDrawElementsInstanced)
        Return functionDel
    End Function
    Function GetFunctionDrawArraysInstanced(name As String) As glDrawArraysInstanced
        Dim functionDel As glDrawArraysInstanced = AddressOf glDrawArraysInstancedPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glDrawArraysInstanced)
        Return functionDel
    End Function
    Function GetFunctionVertexAttribDivisor(name As String) As glVertexAttribDivisor
        Dim functionDel As glVertexAttribDivisor = AddressOf glVertexAttribDivisorPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glVertexAttribDivisor)
        Return functionDel
    End Function
    Function GetFunctionDrawBuffers(name As String) As glDrawBuffers
        Dim functionDel As glDrawBuffers = AddressOf glDrawBuffersPlaceholder
        Dim address As IntPtr
        address = wglGetProcAddress(strToPtr(name))
        functionDel = CType(Marshal.GetDelegateForFunctionPointer(address, functionDel.GetType()), glDrawBuffers)
        Return functionDel
    End Function

    '// Convert a string to a pointer to a null-terminated string
    Function strToPtr(data As String) As IntPtr
        Dim pointer As IntPtr = Marshal.AllocHGlobal(data.Length + 1)
        For i = 0 To data.Length - 1
            Marshal.WriteByte(pointer + i, CByte(AscW(data(i))))
        Next
        Marshal.WriteByte(pointer + data.Length, 0)
        Return pointer
    End Function

    '// Function import from DLL

    <DllImport("opengl32.dll")>
    Shared Function wglGetProcAddress(name As IntPtr) As IntPtr
    End Function
End Class

