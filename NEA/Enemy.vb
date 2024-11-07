Option Strict On
Imports System.Runtime.InteropServices
Imports NEA.CoordDataTypes
'// Class to represent a single enemy
Public Class Enemy
    Inherits Mob
    Public currentState As States
    Private timeSinceDirectionChange As Single = 0
    Private timeSinceDeath As Single = 0
    Private rotSin As Single = 0.5
    Private rotCos As Single = 0.8
    Private character As Player
    Private isAttacking As Boolean
    Private attackDamageDealt As Boolean
    Private timeSinceAttack As Single
    Private timeSinceGrowl As Single
    Private timeForGrowl As Single
    Public enemyType As Integer
    Private advanceAnimation As Boolean
    Public speed As Single = 1
    Public displayHealthBar As Boolean
    Private soundDeath As Integer
    Public attackDamage As Integer

    Const ATTACK_COOLDOWN As Single = 2

    '// Create new enemy instance, with references to the player and the audio control
    Sub New(ByRef inModel As GLTFModel, ByRef inCharacter As Player, ByRef audio As AudioManager, ByVal inEnemyType As Integer)
        MyBase.New(inModel, audio)
        currentState = States.Wander
        animationName = "walk"
        isAttacking = False
        animationList.Clear()
        character = inCharacter
        advanceAnimation = True
        soundDeath = audio.LoadResource("Death.wav")
        enemyType = inEnemyType
    End Sub

    '// Copy instance data to an array of bytes
    Public Function GetSaveDataByte() As Byte()
        Dim saveStruct As EnemySaveData = GetSaveDataStructure()
        Dim saveStructSize As Integer = Marshal.SizeOf(saveStruct)
        Dim saveData(saveStructSize - 1) As Byte
        Dim saveDataPtr As IntPtr
        '// Cast structure to byte array
        saveDataPtr = Marshal.AllocHGlobal(saveStructSize)
        Marshal.StructureToPtr(saveStruct, saveDataPtr, False)
        Marshal.Copy(saveDataPtr, saveData, 0, saveData.Length)
        Marshal.FreeHGlobal(saveDataPtr)
        Return saveData
    End Function

    '// Create a save data record for instance data
    Private Function GetSaveDataStructure() As EnemySaveData
        Dim saveData As New EnemySaveData()
        saveData.health = health
        saveData.maxHealth = maxHealth
        saveData.location = location
        saveData.size = size
        saveData.type = enemyType
        Return saveData
    End Function

    '// Load data when loading a game
    Public Sub LoadFromSaveData(saveData As EnemySaveData)
        health = saveData.health
        location = saveData.location
        maxHealth = saveData.maxHealth
        size = saveData.size
        enemyType = saveData.type
    End Sub

    '// Outline enemy if if can be attacked
    Protected Overrides Function GetHighlightColour() As COORD3Sng
        Dim isHighlighted As Boolean = False
        '// Outline enemy if it is closest
        If closest Then
            isHighlighted = True
        End If
        '// Outline all enemies in range of the character if a splash weapon is used
        If character.weapon.baseData.attackType = Inventory.DamageType.Splash Then
            If InRange(character.location, character.cameraRotation, Player.ATTACK_RANGE) Then
                isHighlighted = True
            End If
        End If
        If isHighlighted Then
            Return New COORD3Sng(1, 0, 0)
        Else
            Return New COORD3Sng(0, 0, 0)
        End If
    End Function

    '// Prevent enemies from intersecting
    Private Sub MaintainDistance(deltaT As Single, otherEnemies As Enemy(), myIndex As Integer)
        Dim force As New COORD3Sng(0, 0, 0)
        Dim dist As Single
        Dim fractX, fractZ As Single
        For i = 0 To otherEnemies.Length - 1
            '// If close to something different
            '// Apply a force away from the collision
            '// Force decreases with distance
            If i <> myIndex Then
                dist = Distance(location, otherEnemies(i).location)
                fractX = (location.x - otherEnemies(i).location.x) / dist
                fractZ = (location.z - otherEnemies(i).location.z) / dist
                force.x += CSng(Math.Pow(dist, -4) * fractX)
                force.z += CSng(Math.Pow(dist, -4) * fractZ)
            End If
        Next
        '// Clamp forces
        If force.x > 2 Then force.x = 2
        If force.z > 2 Then force.z = 2
        If force.x < -2 Then force.x = -2
        If force.z < -2 Then force.z = -2
        location.x += force.x * deltaT
        location.z += force.z * deltaT
    End Sub

    '// Called once per frame
    Public Sub Update(deltaT As Single, otherEnemies As Enemy(), myIndex As Integer)
        If advanceAnimation Then
            animationProgress += deltaT * model.animations.GetAnimationFrame(animationName).speed / size.y
        End If
        timeSinceAttack += deltaT
        timeSinceGrowl += deltaT

        '// Make growl noise
        If timeSinceGrowl > timeForGrowl Then
            audio.PlaySound(soundGrowl, False, True, location, 0.2)
            timeSinceGrowl = 0
            timeForGrowl = CSng(Rnd() * 10) + 5
        End If

        '// Specific actions
        Select Case currentState
            Case States.Wander
                Wander(deltaT)
                MaintainDistance(deltaT, otherEnemies, myIndex)
                bypassAnimationTranslation = True
            Case States.Chase
                Chase(deltaT)
                MaintainDistance(deltaT, otherEnemies, myIndex)
                bypassAnimationTranslation = True
            Case States.Attack
                Attack(deltaT)
                bypassAnimationTranslation = False
            Case States.Death
                DeathAnimation(deltaT)
                bypassAnimationTranslation = False
        End Select

        '// Set death state
        If health <= 0 And currentState <> States.Death Then
            currentState = States.Death
            character.KilledEnemy(enemyType)
            audio.PlaySound(soundDeath, False, True, location, 0.3)
            GiveLoot(character)
            animationName = "death"
            animationProgress = 0
            canBeTargeted = False
        End If
        UpdateAnimationPosition(Not bypassAnimationTranslation)
        If bypassAnimationTranslation Then
            location.x += rotSin * deltaT
            location.z += rotCos * deltaT
        End If
        displayHealthBar = CSng(Timer) - lastHurt < 1 And health > 0
        SmoothRotation(deltaT)
    End Sub

    '// Called while in attack state
    Private Sub Attack(deltaT As Single)
        '// Check for range - prevent intersection with player
        Dim closeness As Single = 0.7
        If character.CanBlock(location) Then
            closeness = 1.2
        End If
        If CloseToPlayer(closeness) Then
            location.x = character.location.x - rotSin * (closeness - 0.05F)
            location.z = character.location.z - rotCos * (closeness - 0.05F)
        End If
        '// Perfrm actions
        If isAttacking Then
            If animationProgress > model.animations.GetAnimationFrame(animationName).data.Length / 20 Then
                EndAttack()
            Else
                If Not attackDamageDealt And animationProgress > model.animations.GetAnimationFrame(animationName).attack / 20 Then
                    DealDamage()
                End If
            End If
        Else
            InitialiseAttack()
        End If
    End Sub

    '// Occurs once per attack
    Private Sub DealDamage()
        '// Check for range
        If CloseToPlayer(1.3) Then
            character = character
            '// Check for shield
            If Rnd() > Player.ShieldChance(character.shield.specificData.level, character.shield.baseData.power) Or Not character.CanBlock(location) Then
                '// Deal damage
                character.health -= attackDamage
                character.lastOof = CSng(Timer)
                audio.PlaySound(soundAttack, False, True, location, 0.5)
                '// Play shield pierce animation
                If character.CanBlock(location) Then
                    character.actionInProgress = False
                    character.animationName = "blockbreak"
                    character.animationProgress = 0
                    character.animationList.Clear()
                End If
            End If
        End If
        attackDamageDealt = True
    End Sub

    '// Transition from attack state
    Private Sub EndAttack()
        isAttacking = False
        animationName = "walk"
        animationList.Clear()
        currentState = States.Chase
        timeSinceAttack = 0
    End Sub

    '// Transition to attack state
    Private Sub InitialiseAttack()
        isAttacking = True
        attackDamageDealt = False
        animationName = "punch"
        animationProgress = 0
        animationList.Clear()
    End Sub

    '// Follow player
    Private Sub Chase(deltaT As Single)
        Dim closeness As Single = 0.7
        If character.CanBlock(location) Or character.animationName = "blockbreak" Then
            closeness = 1.2
        End If
        '// Point at player
        rotation = CSng(Math.Atan2(character.location.x - location.x, character.location.z - location.z))
        rotSin = CSng(Math.Sin(rotation))
        rotCos = CSng(Math.Cos(rotation))
        '// Prevent intersection with player and floor
        If CloseToPlayer(closeness) Then
            location.x = character.location.x - rotSin * (closeness - 0.05F)
            location.z = character.location.z - rotCos * (closeness - 0.05F)
        End If
        location.y = PerlinNoise.GetHeight(location.x, location.z, GameWorld.randoms) * 100
        '// State transitions
        If Not CloseToPlayer(30) Then
            currentState = States.Wander
        End If
        If CloseToPlayer(closeness + 0.3F) And timeSinceAttack > ATTACK_COOLDOWN Then
            currentState = States.Attack
        End If
    End Sub

    '// Called once per frame
    Private Sub Wander(deltaT As Single)
        timeSinceDirectionChange += deltaT
        '// Pick a direction at random and move
        '// Not done every frame to prevent wiggling
        If timeSinceDirectionChange > 5 And identifier = 0 Then
            rotation = Rnd() * 10
            timeSinceDirectionChange = 0
            rotSin = CSng(Math.Sin(rotation))
            rotCos = CSng(Math.Cos(rotation))
        End If
        location.y = PerlinNoise.GetHeight(location.x, location.z, GameWorld.randoms) * 100
        If CloseToPlayer(30) Then
            currentState = States.Chase
        End If
    End Sub

    '// Play death animation or freeze in death position
    Private Sub DeathAnimation(deltaT As Single)
        timeSinceDeath += deltaT
        If animationProgress > (model.animations.GetAnimationFrame(animationName).data.Length - 1) / 20 Then
            advanceAnimation = False
            animationProgress = CSng((model.animations.GetAnimationFrame(animationName).data.Length - 1) / 20)
        End If
        If timeSinceDeath > 10 Then
            wasKilled = True
        End If
    End Sub

    Private Function CloseToPlayer(distanceRequired As Single) As Boolean
        Return Distance(location, character.location) < distanceRequired
    End Function

    '// Structures and enums

    Public Structure EnemySaveData
        Public location As COORD3Sng
        Public size As COORD3Sng
        Public health As Integer
        Public maxHealth As Integer
        Public type As Integer
    End Structure

    Enum States
        Wander = 0
        Chase = 1
        Attack = 2
        Death = 3
    End Enum
End Class

