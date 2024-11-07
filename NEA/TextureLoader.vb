Option Strict On
Imports NEA.OpenGLImporter
Imports System.Runtime.InteropServices
'// Provides a wrapper to load a texture
Public Class TextureLoader
    Public Shared context As OpenGLContext

    '// Provides a wrapper for a texture atlas
    '// Contains multiple small textures on one large texture
    Public Class TextureMap
        Public textureData As Byte()
        Public subImages As New List(Of ImageLocation)
        Public textureID As UInteger
        Public height As Integer
        Public width As Integer
        Public currentPosition As ImageLocation
        Private context As OpenGLContext

        '// Initialise texture object
        Sub New(size As CoordDataTypes.COORD2Short, ByRef inContext As OpenGLContext)
            ReDim textureData(CInt(size.x) * size.y * 3 - 1)
            For i = 0 To textureData.Length - 10 Step 3
                textureData(i) = CByte(i / textureData.Length * 200)
            Next
            width = size.x
            height = size.y
            context = inContext
        End Sub

        '// Linear search for a sub image in the main texture
        Public Function GetSubImage(subImageName As String) As ImageLocationSng
            If subImageName = " " Then subImageName = "Space"

            For i = 0 To subImages.Count - 1
                If subImages(i).name = subImageName Then
                    '// Convert pixel location to relative normalised location
                    Return subImages(i).Scale(height, width)
                End If
            Next
            '// Default texture
            Return subImages(0).Scale(height, width)
        End Function

        '// Add all textures in a folder to the atlas
        Public Sub AddTexturesInDirectory(directoryName As String)
            Dim textures As String() = IO.Directory.GetFiles(directoryName)
            For i = 0 To textures.Length - 1
                '// Add all bitmap files in the folder
                If textures(i).Contains(".bmp") Then
                    AddTexture(textures(i))
                End If
            Next
        End Sub

        '// Add a single texture to the texture atlas
        Public Sub AddTexture(textureName As String)
            '// Load image data
            Dim textureBmp As New Bitmap(textureName)
            Dim newImageLocation As New ImageLocation
            Dim pixelPositionTarget As Integer
            Dim pixelPositionSource As Integer

            '// If the image would extend beyond the limits of the current texture, move to next line
            If textureBmp.width + currentPosition.left > width Then
                currentPosition.left = 0
                currentPosition.top += textureBmp.height
            End If

            '// Copy texture data from image to atlas
            For iy = 0 To textureBmp.height - 1
                For ix = 0 To textureBmp.width - 1
                    pixelPositionTarget = (currentPosition.top + iy) * width + currentPosition.left + ix
                    pixelPositionSource = textureBmp.width * iy + ix
                    For j = 0 To 2
                        textureData(pixelPositionTarget * 3 + j) = textureBmp.data(pixelPositionSource * 3 + 2 - j)
                    Next
                Next
            Next

            '// Store the current location of the image so it can be extracted
            newImageLocation.left = currentPosition.left
            newImageLocation.top = currentPosition.top
            newImageLocation.width = textureBmp.width
            newImageLocation.height = textureBmp.height
            newImageLocation.name = textureName.Split("\"c)(textureName.Split("\"c).Length - 1).Split("."c)(0)
            If newImageLocation.name.Length = 2 AndAlso newImageLocation.name(1) = "1" Then
                newImageLocation.name = newImageLocation.name(0)
            End If

            subImages.Add(newImageLocation)

            '// Move the position of the next image right
            currentPosition.left += newImageLocation.width
        End Sub

        '// Copy texture atlas to OpenGL texture and apply default settings
        Public Sub LoadTexture(textureID As UInt32, minFilter As Int32, magFilter As Int32, Optional mipmap As Boolean = False)
            Dim texturePtr As IntPtr = Marshal.AllocHGlobal(textureData.Length)
            Marshal.Copy(textureData, 0, texturePtr, textureData.Length)

            context.glBindTexture(GL_TEXTURE_0, textureID)
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, texturePtr)
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, minFilter)
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, magFilter)
            If mipmap Then glGenerateMipmap(GL_TEXTURE_2D)

            Marshal.FreeHGlobal(texturePtr)
        End Sub

        '// Structure to store the position of an image in a texture atlas in pixels
        Structure ImageLocation
            Public name As String
            Public top As Integer
            Public left As Integer
            Public width As Integer
            Public height As Integer
            '// Convert pixels to normalised texture coordinates
            Public Function Scale(scaleHeight As Integer, scaleWidth As Integer) As ImageLocationSng
                Dim scaled As New ImageLocationSng
                scaled.top = CSng(top / scaleHeight)
                scaled.width = CSng(width / scaleWidth)
                scaled.height = CSng(height / scaleHeight)
                scaled.left = CSng(left / scaleWidth)
                Return scaled
            End Function
        End Structure

        '// Structure to store location of image in normalised texture space
        Structure ImageLocationSng
            Public top As Single
            Public left As Single
            Public width As Single
            Public height As Single
        End Structure

    End Class

    '// Bind multiple textures from an array
    Public Shared Sub BindTextures(textures As UInteger())
        For i = 0 To textures.Length - 1
            context.glBindTexture(CUInt(GL_TEXTURE_0 + i), textures(i))
        Next
    End Sub

    '// Load a texture from a file to OpenGL texture
    Public Shared Sub LoadTexture2D(textureName As String, textureID As UInt32, minFilter As Int32, magFilter As Int32, Optional mipmap As Boolean = False, Optional invert As Boolean = True)
        Dim textureMapPtr As IntPtr
        '// Load texture data
        Dim texture As Bitmap = LoadTextureToMemory(textureName, textureMapPtr, invert)
        context.glBindTexture(GL_TEXTURE_0, textureID)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, texture.width, texture.height, 0, GL_RGB, GL_UNSIGNED_BYTE, textureMapPtr)
        '// Apply default settings
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, minFilter)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, magFilter)
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE)
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE)

        '// Used for downsampling textures for faster display if an object is far away
        If mipmap Then glGenerateMipmap(GL_TEXTURE_2D)

        Marshal.FreeHGlobal(textureMapPtr)
    End Sub

    '// Load a cube map texture
    '// A cube map consists of 6 separate textures and is sampled with a 3D vector
    Public Shared Sub LoadTextureCubeMap(textureName As String, textureID As UInt32, minFilter As Int32, magFilter As Int32)
        Dim textureMapPtr As IntPtr
        Dim texture As Bitmap = LoadTextureToMemory(textureName, textureMapPtr)
        glBindTexture(GL_TEXTURE_CUBE_MAP, textureID)
        glEnable(GL_TEXTURE_2D)
        '// Copy a section of the image into each face
        For i = 0 To 5
            glTexImage2D(CUInt(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i), 0, GL_RGB, texture.width, texture.height \ 6, 0, GL_RGB, GL_UNSIGNED_BYTE, textureMapPtr + i * texture.width * texture.height \ 2)
        Next

        '// Set default interpolation and wrapping
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, minFilter)
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, magFilter)
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE)
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE)
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE)

        Marshal.FreeHGlobal(textureMapPtr)
    End Sub

    '// Load texture from file to location in memory
    Private Shared Function LoadTextureToMemory(textureName As String, ByRef textureMapPtr As IntPtr, Optional invert As Boolean = True) As Bitmap
        '// Load image to bitmap object
        Dim texture As New Bitmap(textureName, invert)
        '// Copy data to memory
        textureMapPtr = Marshal.AllocHGlobal(texture.data.Length)
        Marshal.Copy(texture.data, 0, textureMapPtr, texture.data.Length)
        Return texture
    End Function

End Class

