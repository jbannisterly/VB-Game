Option Strict On
Imports System.Console
Imports System.Runtime.InteropServices
Imports NEA.CoordDataTypes
Public Class AudioManager
    Public Const ERROR_MESSAGE As String = "Something went wrong. Error code "
    Public Const AUDIO_PATH As String = "Resources\Sounds\"

    Private header As WaveHdr()
    Private headerInUse As Boolean()
    Private resources As List(Of SoundResource)
    Private sampleRate As UInteger
    Private sampleDepth As UInteger
    Private waveforms As Short()()
    Private waveformPtr As IntPtr()
    Private chunkSize As Integer
    Private waveoutHandle As UInteger
    Public currentSounds As New List(Of SoundInstance)
    Private updateRequired As Boolean
    Private loadedBufferIndex As Integer
    Private soundCount As Integer

    Private bufferCount As Integer

    Public listenerLoc As COORD3Sng
    Public listenerRot As Single

    Sub New(numHeaders As Integer, inSampleRate As UInteger, inSampleDepth As Integer, inChunkSize As Integer)
        ReDim header(numHeaders - 1)
        ReDim headerInUse(numHeaders - 1)
        ReDim waveforms(numHeaders - 1)
        ReDim waveformPtr(numHeaders - 1)
        resources = New List(Of SoundResource)
        chunkSize = inChunkSize
        sampleRate = inSampleRate
        waveoutHandle = InitialiseWaveout(sampleRate, 0)

        '// Allocate memeory for each header
        For i = 0 To numHeaders - 1
            ReDim waveforms(i)(chunkSize - 1)
            waveformPtr(i) = Marshal.AllocHGlobal(chunkSize * 2)
            header(i) = InitialiseHeader(waveoutHandle, waveformPtr(i), CUInt(chunkSize), CUInt(i))
        Next
        updateRequired = False
        loadedBufferIndex = 1
        AudioManagerCallback.currentBufferIndex = 0
    End Sub

    Public Sub PlayAudio()
        Dim bufferToEdit As Integer
        Dim result As UInteger
        Dim s As New Stopwatch
        Dim currentHeader As WaveHdr = InitialiseHeader(waveoutHandle, IntPtr.Zero, CUInt(chunkSize), 0)
        Dim currentBufferIndexLocal As Integer
        '// Used for debugging
        s.Start()

        Do
            loadedBufferIndex = loadedBufferIndex Mod header.Length
            currentBufferIndexLocal = AudioManagerCallback.currentBufferIndex Mod header.Length
            '// Check if buffer is free
            If currentBufferIndexLocal <> loadedBufferIndex Then
                bufferToEdit = loadedBufferIndex
                bufferCount += 1
                '// Copy new data to buffer
                LoadNewBuffer(bufferToEdit, bufferCount, listenerLoc, listenerRot)
                Marshal.Copy(waveforms(bufferToEdit), 0, waveformPtr(bufferToEdit), waveforms(bufferToEdit).Length)
                loadedBufferIndex += 1
                currentHeader = header(bufferToEdit)
                '// Send buffer to be played
                result = waveOutUnprepareHeader(waveoutHandle, currentHeader, CUInt(Marshal.SizeOf(currentHeader)))
                If result <> 0 Then WriteLine("Oops a " & result)
                result = waveOutPrepareHeader(waveoutHandle, currentHeader, CUInt(Marshal.SizeOf(currentHeader)))
                If result <> 0 Then WriteLine("Oops b " & result)
                result = waveOutWrite(waveoutHandle, currentHeader, CUInt(Marshal.SizeOf(currentHeader)))
                If result <> 0 Then WriteLine("Oops c " & result)
                CleanUpAudio(currentSounds, bufferCount)
            End If
        Loop
    End Sub

    Private Sub CleanUpAudio(ByRef currentSoundInstances As List(Of SoundInstance), ByVal bufferOffset As Integer)
        Dim i As Integer = 0
        '// Delete sounds that have finished playing
        While i < currentSoundInstances.Count
            If (bufferOffset - currentSoundInstances(i).startChunk) * chunkSize > resources(currentSounds(i).soundID).data.Length And Not currentSoundInstances(i).looping Then
                currentSoundInstances.RemoveAt(i)
            Else
                i += 1
            End If
        End While
    End Sub

    Private Function DistanceSquare(a As COORD3Sng, b As COORD3Sng) As Single
        Return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z)
    End Function

    Private Sub LoadNewBuffer(bufferID As Integer, bufferOffset As Integer, listenerLocation As COORD3Sng, listenerRotation As Single)
        '// Generate data for audio based on poisiton in the world
        Dim waveform(chunkSize - 1) As Integer
        Dim baseWaveform As Integer
        Dim intensity As Single
        Dim theta As Single
        Const SOUND_RANGE As Single = 10
        For i = 0 To currentSounds.Count - 1
            '// Modifiy intensity on left and right channels based on world position
            If currentSounds(i).inWorld Then
                intensity = SOUND_RANGE * SOUND_RANGE / DistanceSquare(listenerLocation, currentSounds(i).worldPosition)
                intensity = Math.Min(intensity, 1) * currentSounds(i).baseVolume
                theta = CSng(Math.Atan2(currentSounds(i).worldPosition.x - listenerLocation.x, currentSounds(i).worldPosition.z - listenerLocation.z))
                theta -= listenerRotation
                While theta > Math.PI
                    theta -= CSng(2 * Math.PI)
                End While
                While theta < -Math.PI
                    theta += CSng(2 * Math.PI)
                End While
                currentSounds(i).left = (0.5F - 0.5F * CSng(Math.Sin(theta))) * intensity
                currentSounds(i).right = (0.5F + 0.5F * CSng(Math.Sin(theta))) * intensity
                If Math.Abs(theta) > Math.PI * 0.5 Then
                    currentSounds(i).left *= -1
                End If
            End If
            '// Add values to the array storing magnitudes
            For j = 0 To waveform.Length - 1
                If j + (bufferOffset - currentSounds(i).startChunk) * chunkSize < resources(currentSounds(i).soundID).data.Length Or currentSounds(i).looping Then
                    baseWaveform = resources(currentSounds(i).soundID).data((j + (bufferOffset - currentSounds(i).startChunk) * chunkSize) Mod resources(currentSounds(i).soundID).data.Length)
                    If j Mod 2 = 0 Then
                        waveform(j) += CInt(baseWaveform * currentSounds(i).left)
                    Else
                        waveform(j) += CInt(baseWaveform * currentSounds(i).right)
                    End If
                End If
            Next
        Next
        '// Prevent overflow errors
        For i = 0 To waveform.Length - 1
            If waveform(i) > 32767 Then waveform(i) = 32767
            If waveform(i) < -32768 Then waveform(i) = -32768
            waveforms(bufferID)(i) = CShort(waveform(i))
        Next
    End Sub

    Public Sub StopSound(ID As Integer)
        '// Delete sound by ID
        Dim i As Integer = 0
        While i < currentSounds.Count
            If currentSounds(i).instanceID = ID Then
                currentSounds.RemoveAt(i)
            Else
                i += 1
            End If
        End While
    End Sub

    Public Function PlaySound(ID As Integer, looping As Boolean, inWorld As Boolean, worldPos As COORD3Sng, volume As Single) As Integer
        '// Add sound to list of sounds and initialise properties
        Dim newSound As New SoundInstance
        soundCount += 1
        newSound.startChunk = bufferCount
        newSound.soundID = ID
        newSound.looping = looping
        newSound.left = volume
        newSound.right = volume
        newSound.baseVolume = volume
        newSound.inWorld = inWorld
        newSound.worldPosition = worldPos
        newSound.instanceID = soundCount
        currentSounds.Add(newSound)
        updateRequired = True
        Return soundCount
    End Function

    Private Function InitialiseWaveout(sampleRate As UInteger, outputID As UInteger) As UInteger
        '// Create a new WaveFormatex structure and initialise sample rate etc.
        Dim format As New WaveFormatex
        Dim identifier As UInteger
        Dim result As UInteger
        format.cbSize = 0
        format.nChannels = 2
        format.nSamplesPerSec = sampleRate
        format.wBitsPerSample = 16
        format.nBlockAlign = CUShort(format.wBitsPerSample * format.nChannels \ 8)
        format.nAvgBytesPerSec = format.nBlockAlign * format.nSamplesPerSec
        format.wFormatTag = 1
        result = waveOutOpen(identifier, outputID, format, AudioManagerCallback.waveOutProcInstance, 0, 196608)
        If result <> 0 Then WriteLine(ERROR_MESSAGE & result)
        Return identifier
    End Function

    Private Function InitialiseHeader(ByVal handle As UInteger, ByVal dataPtr As IntPtr, ByVal bufferSize As UInteger, ByVal identifier As UInteger) As WaveHdr
        '// Create a new header for waveout buffer
        Dim header As New WaveHdr
        Dim result As UInteger
        header.dwBufferLength = CUInt(bufferSize * 2)
        header.dwUser = identifier
        header.lpData = dataPtr
        result = waveOutPrepareHeader(handle, header, CUInt(Marshal.SizeOf(header)))
        If result <> 0 Then WriteLine(ERROR_MESSAGE & result)
        Return header
    End Function

    Public Function LoadResource(fileName As String) As Integer
        fileName = AUDIO_PATH & fileName
        For i = 0 To resources.Count - 1
            If resources(i).name = fileName Then Return i
        Next
        resources.Add(LoadNewResource(fileName))
        Return resources.Count - 1
    End Function

    Private Function LoadNewResource(fileName As String) As SoundResource
        '// Import sound resource from wav file
        Dim newResource As New SoundResource
        Dim rawData As Byte()
        Dim header As New WavHeader

        newResource.name = fileName
        newResource.data = {}
        If IO.File.Exists(fileName) Then
            '// Import resource
            rawData = IO.File.ReadAllBytes(fileName)
            header = GetWavHeader(rawData)
            newResource.data = GetWavData(rawData, header)
        Else
            WriteLine("Could not load " & fileName)
        End If
        Return newResource
    End Function

    Private Function GetWavData(rawData As Byte(), header As WavHeader) As Short()
        Dim dataMono((rawData.Length - 44) * 8 \ header.bitsPerSample - 1) As Short
        Dim dataStereo As Short()
        Dim tempData As Integer
        '// Convert data to signed 16 bit integers
        Select Case header.bitsPerSample
            Case 8
                Array.Copy(rawData, 44, dataMono, 0, dataMono.Length)
                For i = 0 To dataMono.Length - 1
                    dataMono(i) = CShort(dataMono(i) * 256 - 32768)
                Next
            Case 16
                For i = 0 To dataMono.Length - 1
                    tempData = rawData(i * 2 + 44) + 256 * rawData(i * 2 + 45)
                    '// If MSB is set then the number is negative
                    If tempData > 32767 Then
                        tempData = tempData - 65536
                    End If
                    dataMono(i) = CShort(tempData)
                Next
        End Select
        '// Convert data to stereo
        Select Case header.numChannels
            Case 1
                ReDim dataStereo(dataMono.Length * 2 - 1)
                For i = 0 To dataMono.Length - 1
                    dataStereo(i * 2) = dataMono(i)
                    dataStereo(i * 2 + 1) = dataMono(i)
                Next
            Case 2
                ReDim dataStereo(dataMono.Length - 1)
                Array.Copy(dataMono, dataStereo, dataStereo.Length)
            Case Else
                dataStereo = {}
        End Select
        Return dataStereo
    End Function

    Private Function GetWavHeader(rawData As Byte()) As WavHeader
        '// Copy raw data to memory and cast to WavHeader
        Dim headerPtr As IntPtr = Marshal.AllocHGlobal(64)
        Dim header As New WavHeader
        Marshal.Copy(rawData, 0, headerPtr, 44)
        header = CType(Marshal.PtrToStructure(headerPtr, GetType(WavHeader)), WavHeader)
        Marshal.FreeHGlobal(headerPtr)
        Return header
    End Function

    '// Structure declaration

    <StructLayout(LayoutKind.Sequential)>
    Structure WavHeader
        Public chunkID As Integer
        Public chunkSize As Integer
        Public format As Integer
        Public subChunk1ID As Integer
        Public subChunk1Size As Integer
        Public audioFormat As Short
        Public numChannels As Short
        Public sampleRate As Integer
        Public byteRate As Integer
        Public blockAlign As Short
        Public bitsPerSample As Short
        Public subChunk2ID As Integer
        Public subChunk2Size As Integer
    End Structure

    Class SoundInstance
        Public soundID As Integer
        Public instanceID As Integer
        Public startChunk As Integer
        Public looping As Boolean
        Public left As Single
        Public right As Single
        Public baseVolume As Single
        Public inWorld As Boolean
        Public worldPosition As COORD3Sng
    End Class

    Structure SoundResource
        Public name As String
        Public data As Short()
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Structure WaveFormatex
        Public wFormatTag As UShort
        Public nChannels As UShort
        Public nSamplesPerSec As UInteger
        Public nAvgBytesPerSec As UInteger
        Public nBlockAlign As UShort
        Public wBitsPerSample As UShort
        Public cbSize As UShort
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Structure WaveHdr
        Public lpData As IntPtr
        Public dwBufferLength As UInteger
        Public dwBytesRecorded As UInteger
        Public dwUser As UInteger
        Public dwFlags As UInteger
        Public dwLoops As UInteger
        Public lpNext As UIntPtr
        Public reserved As UInteger
    End Structure

    '// Import functions from dll

    <DllImport("Winmm.dll")>
    Private Shared Function waveOutOpen(ByRef phwo As UInteger, ByVal uDeviceID As UInteger, ByRef pwfx As WaveFormatex, ByVal callback As AudioManagerCallback.waveOutProcDel, ByVal dwInstance As UInteger, ByVal fdwOpen As UInteger) As UInteger
    End Function

    <DllImport("Winmm.dll")>
    Private Shared Function waveOutWrite(ByVal hwo As UInteger, ByRef pwh As WaveHdr, ByVal cbwh As UInteger) As UInteger
    End Function

    <DllImport("Winmm.dll")>
    Private Shared Function waveOutPrepareHeader(ByVal hwo As UInteger, ByRef pwh As WaveHdr, ByVal cbwh As UInteger) As UInteger
    End Function

    <DllImport("Winmm.dll")>
    Private Shared Function waveOutUnprepareHeader(ByVal hwo As UInteger, ByRef pwh As WaveHdr, ByVal cbwh As UInteger) As UInteger
    End Function


    <DllImport("Winmm.dll")>
    Private Shared Function waveOutGetVolume(ByVal hwo As UInteger, ByRef pdwVolume As UShort) As UInteger
    End Function

    <DllImport("Winmm.dll")>
    Private Shared Function waveOutGetNumDevs() As UInteger
    End Function
End Class

