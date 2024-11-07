Option Strict On
Imports NEA.CoordDataTypes
Imports System.Runtime.InteropServices
'// Class to update all enemies and control actions and spawn
Public Class EnemyManager
    Const DESPAWN_DISTANCE As Single = 100
    Const MAX_ENEMIES As Integer = 0
    Public enemies As New List(Of Enemy)
    Public enemyModels As ModelManager
    Public randoms As Single()
    Private character As Player
    Private context As OpenGLContext
    Private audio As AudioManager
    Public enemyEncyclopedia As EnemyInfo()

    '// Initialise references
    Sub New(inCharacter As Player, GLTFProgram As Integer, inRandoms As Single(), ByRef inContext As OpenGLContext, ByRef inAudio As AudioManager, ByRef inModels As ModelManager)
        context = inContext
        character = inCharacter
        enemyModels = inModels
        randoms = inRandoms
        audio = inAudio
        '// Load enemy type data from a file
        enemyEncyclopedia = LoadEnemyTypes()
    End Sub

    '// Outputs a byte array to be saved in a file
    Public Function GetSaveData() As Byte()
        Dim rawData As New List(Of Byte)
        For i = 0 To enemies.Count - 1
            rawData.AddRange(enemies(i).GetSaveDataByte())
        Next
        Return rawData.ToArray()
    End Function

    '// Iterate through save file and spawn saved enemies
    Public Sub LoadFromSaveData(rawData As Byte())
        Dim enemyInstance As New Enemy.EnemySaveData
        Dim enemyInstanceSize As Integer = Marshal.SizeOf(enemyInstance)
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(rawData.Length)
        Marshal.Copy(rawData, 0, dataPtr, rawData.Length)
        enemies.Clear()
        For i = 0 To rawData.Length \ enemyInstanceSize - 1
            '// Cast data to EnemySaveData type
            enemyInstance = CType(Marshal.PtrToStructure(dataPtr + enemyInstanceSize * i, enemyInstance.GetType()), Enemy.EnemySaveData)
            '// Load new enemy
            SpawnEnemy(enemyInstance.type, New COORD3Sng(0, 0, 0))
            enemies(i).LoadFromSaveData(enemyInstance)
        Next
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    '// Get base enemy data from external file
    Private Function LoadEnemyTypes() As EnemyInfo()
        Dim enemyData As New List(Of EnemyInfo)
        Dim enemyNames As String() = IO.Directory.GetFiles("Resources\Enemies")
        For i = 0 To enemyNames.Length - 1
            enemyData.Add(New EnemyInfo(IO.File.ReadAllLines(enemyNames(i))))
        Next
        Return enemyData.ToArray()
    End Function

    '// Call Display on all enemies
    Public Sub RenderEnemies(program As UInteger, ByRef matrixRelative As Matrices, ByRef matrixView As Matrices, ByRef matrixPerspective As Matrices)
        For i = 0 To enemies.Count - 1
            enemies(i).Display(program, matrixRelative, matrixView, matrixPerspective, False, {New Matrices(4, 4, True)}, -1)
        Next
    End Sub

    '// Run once per frame
    Public Sub Update(deltaT As Single)
        Dim closestEnemy As Integer = GetClosest()
        For i = 0 To enemies.Count - 1
            '// Mark closest enemy as such for highlighting and attacks
            If i = closestEnemy Then
                enemies(i).closest = True
            Else
                enemies(i).closest = False
            End If
            '// Update all enemies
            enemies(i).Update(deltaT, enemies.ToArray(), i)
        Next
    End Sub

    '// Returns index of the enemy to be attacked next
    Public Function GetClosest() As Integer
        Dim angle As Single = 0
        Dim closestAngle As Single = 1000000
        Dim closestEnemy As Integer = -1
        For i = 0 To enemies.Count - 1
            If enemies(i).InRange(character.location, character.cameraRotation, Player.ATTACK_RANGE) Then
                angle = enemies(i).GetAngle(character.location, character.cameraRotation)
                '// Enemy is considered closest if it is within range and directly in front of player.
                If angle < closestAngle Then
                    closestAngle = angle
                    closestEnemy = i
                End If
            End If
        Next
        If closestAngle <= 1000 Then
            '// Enemy in range
            Return closestEnemy
        Else
            '// No enemies in range
            Return -1
        End If
    End Function

    '// Creates a new enemy
    Private Sub SpawnRandomEnemy()
        Dim location As New COORD3Sng()
        Randomize()

        '// Random location
        location = character.location
        location.x += Rnd() * 10 - 5
        location.z += Rnd() * 10 - 5

        '// Spawn enemy
        SpawnEnemy(0, location)
    End Sub

    '// Spawn enemy by name
    '// Overload for spawn enemy by index
    Public Sub SpawnEnemy(name As String, location As COORD3Sng)
        Dim index As Integer
        index = GetEnemyIndexByName(name)
        If index <> -1 Then
            SpawnEnemy(index, location)
        End If
    End Sub

    '// Linear search for enemy by index
    '// Performance is not critical as there are very few enemy types
    '// And this is not called often
    Public Function GetEnemyIndexByName(name As String) As Integer
        For i = 0 To enemyEncyclopedia.Length - 1
            If enemyEncyclopedia(i).name = name Then
                Return i
            End If
        Next
        Return -1
    End Function

    '// Create new enemy by index and add to list
    Public Sub SpawnEnemy(index As Integer, location As COORD3Sng)
        enemies.Add(GetSpawnEnemy(enemyEncyclopedia(index), location, index))
    End Sub

    '// Create new enemy by index
    Private Function GetSpawnEnemy(newEnemyType As EnemyInfo, location As COORD3Sng, newEnemyTypeID As Integer) As Enemy
        Dim model As GLTFModel = enemyModels.GetModel(newEnemyType.modelName)
        Dim newEnemy As New Enemy(model, character, audio, newEnemyTypeID)
        '// Initialise location
        newEnemy.location.x = location.x
        newEnemy.location.z = location.z
        newEnemy.location.y = PerlinNoise.GetHeight(newEnemy.location.x, newEnemy.location.z, randoms) * 100
        newEnemy.identifier = enemies.Count
        '// LOad specific type of enemy
        newEnemy.skin = newEnemyType.skin
        newEnemy.maxHealth = newEnemyType.health
        newEnemy.health = newEnemyType.health
        newEnemy.attackDamage = newEnemyType.attack
        newEnemy.enemyType = newEnemyTypeID

        '// Chance to spawn giant version of mob with increased health
        If Rnd() > 0.9 Then
            newEnemy.size = New COORD3Sng(1.5, 1.5, 1.5)
            newEnemy.health *= 3
            newEnemy.maxHealth *= 3
            newEnemy.speed = 0.7
        End If

        Return newEnemy
    End Function

    '// Clean up enemies that have died or are out of range
    Public Sub RefreshSpawn()
        Dim index As Integer = 0

        '// Iterate through each enemy, despawning if necessary
        While index < enemies.Count
            If enemies(index).wasKilled Then
                Despawn(index)
            ElseIf Distance(enemies(index).location, character.location) > DESPAWN_DISTANCE Then
                Despawn(index)
            Else
                index += 1
            End If
        End While

        '// Randomly spawn enemies if the max enemy limit is not reached
        If Rnd() > 0.1 And enemies.Count < MAX_ENEMIES Then
            SpawnRandomEnemy()
        End If
    End Sub

    '// Subroutine to remove an enemy by identifier
    Public Sub Despawn(identifier As Integer)
        enemies.RemoveAt(identifier)
        For i = 0 To enemies.Count - 1
            enemies(i).identifier = i
        Next
    End Sub

    '// Iteratively cast shadows for all enemies
    Public Sub CastShadows(ByRef shadowRenderer As Shadow, shadowModelProgram As Integer, shadowFramebuffer As UInteger, shadowTexturesDynamic As UInteger())
        For i = 0 To enemies.Count - 1
            shadowRenderer.ProjectModelShadows(shadowTexturesDynamic, shadowModelProgram, shadowFramebuffer, enemies(i), False, {New Matrices(4, 4, True)}, -1)
        Next
    End Sub

    '// Structures

    Structure EnemyInfo
        Public name As String
        Public health As Integer
        Public attack As Integer
        Public drop As String
        Public modelName As String
        Public skin As Integer
        Sub New(rawData As String())
            name = rawData(0).Split(":"c)(1)
            health = CInt(rawData(1).Split(":"c)(1))
            attack = CInt(rawData(2).Split(":"c)(1))
            drop = rawData(3).Split(":"c)(1)
            modelName = rawData(4).Split(":"c)(1)
            skin = CInt(rawData(5).Split(":"c)(1))
        End Sub
    End Structure
End Class

