#Const CANFLY = False
Imports System.Console
    Imports System.Runtime.InteropServices
    Imports NEA.CoordDataTypes
    Imports NEA.OpenGLImporter
    '// Entry module for the program
    Module Module1
        '// Constant declaration
        Public Const SHADOWRES = 1024
        '// This is number of points in a chunk (so 3x3 square is size 4)
        Public Const CHUNK_SIZE As Integer = 64
        Public Const SHADOW_MAP_SIZE As Integer = 1024
        Public Const RENDER_DISTANCE As Integer = 8
        Public Const CHUNK_INDEX_LENGTH As Integer = (CHUNK_SIZE - 1) * (CHUNK_SIZE - 1) * 6
        Public Const REFLECTION_SCALE As Single = 1
        Public Const GRASS_DISTANCE As Integer = 60
        Public Const LENGTH_OF_DAY As Single = 1800

        '// Program entry point
        Sub Main()
            '// Declaration of classes
            Dim context As New OpenGLContext
            Dim formInterface As GUIInstance
            Dim keyBindings As New KeyboardInput.KeyBinding
            Dim graphicsBindings As New GameWorld.GraphicsSettings
            Dim audio As New AudioManager(8, 44100, 16, 4096)
            '// Play the audio in a separate thread in parallel
            Dim audioThread As New Threading.Thread(AddressOf audio.PlayAudio)
            If Not IO.Directory.Exists("Saved_Games") Then IO.Directory.CreateDirectory("Saved_Games")
            '// Initialisation of classes
            Initialise()
            formInterface = New GUIInstance(Window.GetSize(), context)
            '// Begin playing audio in parallel
            audioThread.Start()
            '// Load and play the game
            GameWorld.MainGame(Menu.InitialiseForms(formInterface, keyBindings, graphicsBindings, context), formInterface, keyBindings, graphicsBindings, context, audio)
        End Sub

        '// Initialise shared classes
        Sub Initialise()
            Window.FullScreen()
            Window.DisableInput()
            MouseInput.Initialise()
            OpenGLImporter.Initialise()
        End Sub

        '// Used for debugging, writes contents of memory to the console
        Sub WriteMem(ptr As IntPtr, count As Integer)
            For i = 0 To count - 1
                Write(Chr(Marshal.ReadByte(ptr + i)))
            Next
        End Sub

        '// Reshow the mouse when the user exits the program
        Public Sub GracefulExit()
            MouseInput.ShowMouseCursor()
            End
        End Sub

        '// Class used for timing and measuring performance
        Class TimeStamp
            Private Shared stmp As Double
            Public Shared Sub Init()
                stmp = Timer
            End Sub
            Public Shared Sub Stamp(ByRef time As Single)
                '// Ensure all draw commands are finished
                glFinish()
                time += Timer - stmp
                stmp = Timer
            End Sub
        End Class
    End Module

