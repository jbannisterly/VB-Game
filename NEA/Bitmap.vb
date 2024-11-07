Option Strict On
Imports System.Console
'// Class for handling image files
Public Class Bitmap
    Public Const TEXTURE_DIRECTORY As String = "Resources\Textures\"
    Public data As Byte()
    Public height As Integer
    Public width As Integer

    '// Create image from an array of RGB values
    '// Used for debugging
    Sub New(inData As Colour(,))
        height = inData.GetLength(1)
        width = inData.GetLength(0)
        ReDim data(inData.Length * 3 - 1)
        For i = 0 To height - 1
            For j = 0 To width - 1
                data((i * width + j) * 3) = inData(j, i).b
                data((i * width + j) * 3 + 1) = inData(j, i).g
                data((i * width + j) * 3 + 2) = inData(j, i).r
            Next
        Next
    End Sub

    '// Output bitmap to file
    '// Used for debugging
    Sub OutputBitmap(file As String)
        '// Header for all bitmaps
        Dim headerData As Byte() = {66, 77, 0, 0, 0, 0, 0, 0, 0, 0, 54, 0, 0, 0, 40, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 24}
        Dim bmpData(53 + data.Length) As Byte
        Array.Copy(data, 0, bmpData, 54, data.Length)
        Array.Copy(headerData, bmpData, headerData.Length)
        '// Write metadata to header
        WriteInt(CUInt(width), bmpData, 18)
        WriteInt(CUInt(height), bmpData, 22)
        WriteInt(CUInt(bmpData.Length), bmpData, 2)
        IO.File.WriteAllBytes(file & ".bmp", bmpData)
    End Sub

    '// Convert integer to byte array
    Sub WriteInt(ByVal value As UInt32, ByRef data As Byte(), start As Integer)
        For i = 0 To 3
            data(i + start) = CByte(value Mod 256)
            value >>= 8
        Next
    End Sub

    '// Import image from file
    Sub New(file As String)
        Dim rawData As Byte() = IO.File.ReadAllBytes(file.Split("."c)(0) & ".bmp")
        '// Extract important metadata
        height = GetInt(rawData, 22)
        width = GetInt(rawData, 18)
        '// Extract raw data and change ordering from BGR to RGB
        ReDim data(height * width * 3 - 1)
        For i = 0 To height * width - 1
            For j = 0 To 2
                data(3 * i + j) = rawData(3 * i + 54 + 2 - j)
            Next
        Next
    End Sub

    '// Import image from file with option to invert vertically
    '// Textures in OpenGl have (0,0) at bottom left
    '// But bitmaps have (0,0) at top left
    Sub New(file As String, invert As Boolean)
        Dim fullFilePath As String = TEXTURE_DIRECTORY & file
        Dim rawData As Byte()
        '// Load default image if resource not found
        If Not IO.File.Exists(fullFilePath & ".bmp") Then
            If file.Contains("_") Then
                fullFilePath = TEXTURE_DIRECTORY & "Default_" & file.Split("_"c)(1)
            Else
                fullFilePath = TEXTURE_DIRECTORY & "Default"
            End If
        End If
        rawData = IO.File.ReadAllBytes(fullFilePath & ".bmp ")
        '// Get metadata
        height = GetInt(rawData, 22)
        width = GetInt(rawData, 18)
        '// Extract image data, converting BGR to RGB
        ReDim data(height * width * 3 - 1)
        For h = 0 To height - 1
            For w = 0 To width - 1
                For i = 0 To 2
                    If invert Then
                        data(3 * (w + (height - 1 - h) * width) + i) = rawData(3 * (h * width + w) + 54 + 2 - i)
                    Else
                        data(3 * (w + h * width) + i) = rawData(3 * (h * width + w) + 54 + 2 - i)
                    End If
                Next
            Next
        Next
    End Sub

    '// Extract integer from byte array
    Private Function GetInt(ByRef byteData As Byte(), ByVal index As Integer) As Integer
        Dim value As Integer = 0
        For i = 0 To 3
            value += CInt(byteData(index + i)) << (i * 8)
        Next
        Return value
    End Function

    '// Structures
    Public Structure Colour
        Public r As Byte
        Public g As Byte
        Public b As Byte
    End Structure

End Class
