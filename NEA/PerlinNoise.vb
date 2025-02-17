Option Strict On
Imports System.Console
'// Noise generator used to generate terrain
Public Class PerlinNoise

    '// Get an array of 2D unit vectors from a seed
    Public Shared Function GenerateRandomVectors(quantity As Integer, seed As Integer, range As VectorRange) As Single()
        '// Ensure seed is used
        Randomize(seed)
        Dim randoms(quantity * 2 - 1) As Single
        For i = 0 To randoms.Length - 1
            If i Mod 2 = 0 Then
                '// Get random magnitude
                randoms(i) = Rnd()
            Else
                '// Other component must ensure it is a unit vector
                randoms(i) = CSng(Math.Sqrt(1 - randoms(i - 1) * randoms(i - 1)))
            End If
            '// Random direction
            If range = VectorRange.MinusOneToOne AndAlso Rnd() > 0.5 Then randoms(i) *= -1
        Next
        Return randoms
    End Function

    '// Get height of terrain at specified point
    Public Shared Function GetHeight(x As Single, y As Single, randoms As Single()) As Single
        Dim floorX As Single = CSng(Math.Floor(x))
        Dim floorY As Single = CSng(Math.Floor(y))
        '// Get height of four corners of unit square around terrain
        Dim bottomLeft As Single = GetHeightPoint(floorX, floorY, randoms)
        Dim bottomRight As Single = GetHeightPoint(floorX + 1, floorY, randoms)
        Dim topLeft As Single = GetHeightPoint(floorX, floorY + 1, randoms)
        Dim topRight As Single = GetHeightPoint(floorX + 1, floorY + 1, randoms)
        '// Linearly interpolate between heights
        '// This method ensures consistency between CPU and GPU generated terrains
        '// Because the height generated by the shader is done for a unit square vertex and interpolated
        '// The height is stored in a texture and linearly interpolated
        Return Lerp(Lerp(bottomLeft, bottomRight, x - floorX), Lerp(topLeft, topRight, x - floorX), y - floorY)
    End Function

    '// Get height of terrain at a point
    Private Shared Function GetHeightPoint(x As Single, y As Single, randoms As Single()) As Single
        Dim strength As Single() = {0.7, 0.2, 0.5, 0.02}
        Dim scale As Single() = {97, 31, 59, 11}
        Dim height As Single = 0.5
        Dim ocean As Single
        For i = 0 To strength.Length - 1
            '// Add different frequency noise to generate height
            height += GetNoise(x, y, scale(i), randoms) * strength(i)
        Next
        ocean = 1
        If x * x + y * y > 1000000 Then
            '// Create a slope down to the ocean after specified distance
            If x * x + y * y > 1040000 Then
                '// Clamp height to 0 when in ocean
                ocean = 0
            Else
                '// Linear slope down to ocean bed
                ocean = 1 - ((x * x + y * y - 1000000) / 100000)
            End If
        End If
        Return height * ocean
    End Function

    '// Get value of Perlin Noise at a point
    Private Shared Function GetNoise(x As Single, y As Single, scale As Single, randoms As Single()) As Single
        Dim baseValue As Single
        '// Get unit square to consider
        Dim left As Integer = CInt(Math.Floor(x / scale))
        Dim bottom As Integer = CInt(Math.Floor(y / scale))
        Dim fractX As Single = (x / scale) - left
        Dim fractY As Single = (y / scale) - bottom
        Dim leftHeight As Single
        Dim rightHeight As Single

        '// Smoothstep function
        fractX = fractX * fractX * fractX * ((6 * fractX - 15) * fractX + 10)
        fractY = fractY * fractY * fractY * ((6 * fractY - 15) * fractY + 10)

        '// Linearly interpolate between gradients at each vertex
        leftHeight = Lerp(DotProductRandom(randoms, left, bottom, fractX, fractY), DotProductRandom(randoms, left, bottom + 1, fractX, fractY - 1), fractY)
        rightHeight = Lerp(DotProductRandom(randoms, left + 1, bottom, fractX - 1, fractY), DotProductRandom(randoms, left + 1, bottom + 1, fractX - 1, fractY - 1), fractY)
        baseValue = Lerp(leftHeight, rightHeight, fractX) * 0.6F
        Return baseValue
    End Function

    '// Get height due to random gradient at a vertex
    Private Shared Function DotProductRandom(randoms As Single(), coordX As Integer, coordY As Integer, fractX As Single, fractY As Single) As Single
        Dim sampleX, sampleY As Single
        coordX = coordX Mod 128
        coordY = coordY Mod 128
        '// Get random gradient direction
        sampleX = randoms((((128 * coordY) + coordX) * 2 + randoms.Length * 1000) Mod randoms.Length)
        sampleY = randoms((((128 * coordY) + coordX) * 2 + 1 + randoms.Length * 1000) Mod randoms.Length)
        '// Calculate dot product of gradient and location
        Return (sampleX * fractX + sampleY * fractY) * 2 - fractX - fractY
    End Function

    '// Linearly interpolate between a and b
    Private Shared Function Lerp(a As Single, b As Single, distance As Single) As Single
        Return (b - a) * distance + a
    End Function

    '// Structures and enums

    Public Structure NoiseGenPosition
        Public start As Single
        Public stepSize As Single
        Public count As Integer
        Sub New(inStart As Single, inStep As Single, inCount As Integer)
            start = inStart
            stepSize = inStep
            count = inCount
        End Sub
    End Structure

    Public Enum VectorRange
        ZeroToOne = 0
        MinusOneToOne = 1
    End Enum
End Class

