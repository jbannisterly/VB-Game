Option Strict On
Imports System.Runtime.InteropServices
'// Initialise OpenGL context and provide a target
Public Class OpenGL

    Public Shared Sub Initialise()
        Dim context As IntPtr
        Dim glContext As IntPtr
        Dim desc As PIXELFORMATDESCRIPTOR = NewPixDescriptor()
        Dim format As Int32
        '// Get device context to console window
        context = GetDC(GetForegroundWindow())
        '// Specify pixel format
        format = ChoosePixelFormat(context, desc)
        SetPixelFormat(context, format, desc)
        '// Create OpenGL context and set as active drawer
        glContext = wglCreateContext(context)
        wglMakeCurrent(context, glContext)
        '// Specify that pixels are row by row
        glPixelStorei(&HCF5, 1)
    End Sub

    '// Create a new pixel format descriptor for the context
    Private Shared Function NewPixDescriptor() As PIXELFORMATDESCRIPTOR
        Dim pix As New PIXELFORMATDESCRIPTOR
        pix.size = 40
        pix.version = 1
        '// Support OpenGL and draw to window
        pix.flags = 36
        '// Non-indexed
        pix.pixelType = 0
        '// Specify bit depth
        pix.colourBits = 32
        pix.aBits = 8
        pix.depth = 24
        Return pix
    End Function

    '// Structure declaration to be used to set up context pixel format
    <StructLayout(LayoutKind.Sequential)>
    Structure PIXELFORMATDESCRIPTOR
        Dim size As Int16
        Dim version As Int16
        Dim flags As Int32
        Dim pixelType As Byte
        Dim colourBits As Byte
        Dim rBits As Byte
        Dim rShift As Byte
        Dim bBits As Byte
        Dim bShift As Byte
        Dim gBits As Byte
        Dim gShift As Byte
        Dim aBits As Byte
        Dim aShift As Byte
        Dim accrBits As Byte
        Dim accgBits As Byte
        Dim accbBits As Byte
        Dim accaBits As Byte
        Dim depth As Byte
        Dim stencil As Byte
        Dim buffer As Byte
        Dim layerType As Byte
        Dim reserved As Byte
        Dim layerMask As Int32
        Dim visibleMask As Int32
        Dim damageMask As Int32
    End Structure

    '// DLL function imports
    <DllImport("GDI32.dll")>
    Private Shared Function ChoosePixelFormat(ByVal hnd As IntPtr, ByRef desc As PIXELFORMATDESCRIPTOR) As Int32
    End Function

    <DllImport("GDI32.dll")>
    Private Shared Function SetPixelFormat(ByVal hnd As IntPtr, ByVal format As Int32, ByRef desc As PIXELFORMATDESCRIPTOR) As Boolean
    End Function

    <DllImport("User32.dll")>
    Private Shared Function GetDC(hnd As IntPtr) As IntPtr
    End Function

    <DllImport("User32.dll")>
    Private Shared Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("Opengl32.dll")>
    Private Shared Function wglCreateContext(hnd As IntPtr) As IntPtr
    End Function

    <DllImport("Opengl32.dll")>
    Private Shared Function wglMakeCurrent(hndDC As IntPtr, hndGl As IntPtr) As Boolean
    End Function

    <DllImport("OpenGl32.dll")>
    Private Shared Sub glPixelStorei(name As Int32, param As Int32)
    End Sub

End Class

