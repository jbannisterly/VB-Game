Option Strict On
'// Shared class containg function to be called when buffer has finished playing
'// Must be global as pointer to function is used by api
Class AudioManagerCallback
    Public Shared currentBufferIndex As Integer
    Public Shared waveOutProcInstance As waveOutProcDel = AddressOf waveOutProc

    Public Delegate Sub waveOutProcDel(ByVal hwo As UInteger, ByVal uMsg As UInteger, ByVal dwInstance As UInteger, ByVal dwParam1 As UInteger, ByVal dwParam2 As UInteger)

    Public Shared Sub waveOutProc(ByVal hwo As UInteger, ByVal uMsg As UInteger, ByVal dwInstance As UInteger, ByVal dwParam1 As UInteger, ByVal dwParam2 As UInteger)
        Const OPEN As UInteger = 955
        Const CLOSE As UInteger = 956
        Const DONE As UInteger = 957

        Select Case uMsg
            Case OPEN
            Case CLOSE
            Case DONE
                currentBufferIndex += 1
        End Select
    End Sub
End Class
