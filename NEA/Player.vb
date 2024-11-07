Option Strict On
Imports NEA.CoordDataTypes
Imports System.Runtime.InteropServices
'// Class to control the character the player controls
Public Class Player
    Inherits Mob
    '// Constant declarations
    Const BASE_SPEED As Single = 2
    Const CAMERA_HEIGHT As Single = 2.5
    Const CAMERA_DISTANCE As Single = 3
    Const PLAYER_HEIGHT As Single = 1.7
    Const BASE_DAMAGE As Integer = 10
    Const TIME_TO_HEAL As Single = 2
    Public Const ATTACK_RANGE As Single = 1.5
    Public Const TALK_RANGE As Single = 4

    '// Variable declarations
    Private pastLocations As New List(Of Single)
    Public iconVisible As Boolean
    Public respawnPoint As COORD3Sng
    Public onFloor As Boolean
    Public closeToFloor As Boolean
    Public firstPerson As Boolean
    Private yVelocity As Single
    Public hasAttacked As Boolean
    Public stamina As Single = 100
    Public maxStamina As Single = 100
    Public recoveringStamina As Boolean = False
    Public inventoryItems As Inventory
    Public craftingRecipes As Crafting
    Public missionList As Missions
    Public targetFocusIndex As Integer
    Public targetLock As Boolean
    Public weapon As Inventory.ItemInstance
    Public shield As Inventory.ItemInstance
    Public deltaPosition As New COORD3Sng
    Public talkTarget As Integer = -1
    Public hasHit As Boolean = False
    Public lastOof As Single
    Private timeSinceHeal As Single
    Public dodge As Boolean
    Private dodgeCooldown As Single
    Private dodgeBeginY As Single
    Private cameraBone As Integer
    Public keyBindings As KeyboardInput.KeyBinding
    Private attackAnimationIndex As Integer
    Public cameraRotation As Single
    Public cameraElevation As Single
    Public Property cameraPosition As COORD3Sng
        Get
            If firstPerson Then
                '// Set the camera position to the location of the head bone
                If transformationMatricesOrigin.Length > 1 Then
                    Dim baseMatrix As New Matrices(4, 4, True)
                    Dim cameraMatrix As Matrices = GetItemMatrices(baseMatrix)(cameraBone)
                    Return New COORD3Sng(CSng(location.x + Math.Sin(cameraRotation) * cameraMatrix.data(11)), location.y + cameraMatrix.data(7), CSng(location.z + Math.Cos(cameraRotation) * cameraMatrix.data(11)))
                End If
            Else
                '// Set the camera position to point at the player
                Return New COORD3Sng(CSng(location.x - Math.Sin(cameraRotation) * CAMERA_DISTANCE * Math.Cos(cameraElevation)), CSng(location.y + CAMERA_HEIGHT + Math.Sin(cameraElevation) * 2), CSng(location.z - Math.Cos(cameraRotation) * CAMERA_DISTANCE * Math.Cos(cameraElevation)))
            End If
        End Get
        Set(value As COORD3Sng)

        End Set
    End Property

    '// Initialise the player and associated classes
    Sub New(model As GLTFModel, inInventoryForm As GUIObject.Form, inCraftingForm As GUIObject.Form, inMissionForm As GUIObject.Form, ByRef audio As AudioManager)
        MyBase.New(model, audio)

        '// Initialise inventories
        LoadInventory(inInventoryForm)
        LoadCrafting(inCraftingForm, inventoryItems)
        LoadMissions(inMissionForm)
        animationName = "idle"
        cameraBone = model.GetIndexOfBone("Camera", model.nodes)
    End Sub

    '// Display an icon in the bottom right corner if a weapon is equipped
    Public Sub TryDisplayWeaponIcon(program As UInteger, monitorSize As COORD2Short)
        If iconVisible Then
            DisplayWeaponIcon(program, monitorSize)
        End If
    End Sub

    '// Display the weapon icon in the bottom right corner
    Private Sub DisplayWeaponIcon(program As UInteger, monitorSize As COORD2Short)
        Dim matrixRelative, matrixView, matrixPerspective As Matrices
        Dim oldRotation, oldElevation As Single

        '// Generate matrices for icon and scale it
        matrixRelative = MatrixGenerator.GetRelativeMatrix(-0.6, 0.4, -0.5)
        matrixView = MatrixGenerator.GetViewMatrix(matrixRelative, 0, 0)
        matrixPerspective = MatrixGenerator.GetPerspectiveMatrix(matrixView, monitorSize)
        children(0).size = New COORD3Sng(0.15, 0.15, 0.15)
        '// Backup old positions
        oldElevation = children(0).elevation
        oldRotation = children(0).actualRotation
        '// Set position and uniforms
        children(0).actualRotation = Math.PI / 2
        children(0).elevation = Math.PI / 4
        OpenGLImporter.glUniform3f(OpenGLImporter.glGetUniformLocationStr(CInt(program), "playerPos"), 0, 0, 0)
        '// Display icon
        children(0).Display(program, matrixRelative, matrixView, matrixPerspective, False, {New Matrices(4, 4, True)}, -1)
        '// Restore all old positions
        children(0).size = New COORD3Sng(1, 1, 1)
        children(0).actualRotation = oldRotation
        children(0).elevation = oldElevation
    End Sub

    '// Initialise mission handler
    Private Sub LoadMissions(inMissionForm As GUIObject.Form)
        '// Get references to mission GUI objects
        Dim listMissions As GUIObject.TaskList
        Dim listTasks As GUIObject.TaskList

        inMissionForm.GetElementByID(Menu.Controls.ID_lstMissions, listMissions)
        inMissionForm.GetElementByID(Menu.Controls.ID_lstTasks, listTasks)

        '// Initialise missions
        missionList = New Missions(listMissions, listTasks)
    End Sub

    '// Initialise crafting manager
    Private Sub LoadCrafting(inCraftingForm As GUIObject.Form, inInventory As Inventory)
        '// Get references to crafting GUI objects
        Dim craftingDisplay As GUIObject.GridView
        Dim craftingDetail As GUIObject.Label
        Dim craftingDetailImage As GUIObject.RenderTarget
        Dim craftingEquipItem As GUIObject.Button

        inCraftingForm.GetElementByID(Menu.Controls.ID_grdCrafting, craftingDisplay)
        inCraftingForm.GetElementByID(Menu.Controls.ID_lblRecipeDetail, craftingDetail)
        inCraftingForm.GetElementByID(Menu.Controls.ID_renRecipeDetailImage, craftingDetailImage)
        inCraftingForm.GetElementByID(Menu.Controls.ID_btnCraftRecipe, craftingEquipItem)

        '// Initialise crafting
        craftingRecipes = New Crafting(craftingDisplay, craftingDetail, craftingDetailImage, craftingEquipItem, model.context, inInventory)
    End Sub

    '// Check if the camera should be in first person
    Public Sub FirstPersonCheck(currentDisplayMode As GameWorld.DisplayMode, prevMainMenu As Boolean)
        Select Case currentDisplayMode
            Case GameWorld.DisplayMode.MainGame
                If prevMainMenu Then
                    '// Initialise in third person
                    firstPerson = False
                End If
                If KeyboardInput.KeyPressed(9) Then
                    '// Toggle first / third person with tab key
                    firstPerson = Not firstPerson
                End If
            Case GameWorld.DisplayMode.MainMenu
                '// First person if in the main menu
                firstPerson = True
        End Select
    End Sub

    Public Sub InitialiseReferences(program As UInteger, ByRef enemies As EnemyManager)
        '// Initialise references to GLSL programs
        inventoryItems.inventoryDetailImage.GLTFProgram = program
        inventoryItems.inventoryDisplay.SetProgram(program)
        craftingRecipes.craftingDetailImage.GLTFProgram = program
        craftingRecipes.craftingDisplay.SetProgram(program)
        '// Initialise references amongst inventories
        inventoryItems.missionList = missionList
        missionList.inventoryContents = inventoryItems
        missionList.enemies = enemies
        '// Intialise item and mission data from files
        inventoryItems.LoadItemData("Resources\Items", CInt(program), audio)
        craftingRecipes.LoadRecipeData("Resources\Recipes", CInt(program))
        craftingRecipes.SetPage(0)
        missionList.LoadMissionData("Resources\Missions")
    End Sub

    '// Initialise inventory
    Private Sub LoadInventory(inInventoryForm As GUIObject.Form)
        '// Get references to inventory GUI objects
        Dim inventoryDisplay As GUIObject.GridView
        Dim inventoryDetail As GUIObject.Label
        Dim inventoryDetailImage As GUIObject.RenderTarget
        Dim inventoryEquipItem As GUIObject.Button
        Dim inventorySort As GUIObject.DropDown
        Dim inventoryFilter As GUIObject.DropDown

        inInventoryForm.GetElementByID(Menu.Controls.ID_grdInventory, inventoryDisplay)
        inInventoryForm.GetElementByID(Menu.Controls.ID_lblItemDetail, inventoryDetail)
        inInventoryForm.GetElementByID(Menu.Controls.ID_renItemDetailImage, inventoryDetailImage)
        inInventoryForm.GetElementByID(Menu.Controls.ID_btnEquipItem, inventoryEquipItem)
        inInventoryForm.GetElementByID(Menu.Controls.ID_cmbFilter, inventoryFilter)
        inInventoryForm.GetElementByID(Menu.Controls.ID_cmbSort, inventorySort)

        '// Initialise inventory
        inventoryItems = New Inventory(inventoryDisplay, inventoryDetail, inventoryDetailImage, inventoryEquipItem, model.context, inventorySort, inventoryFilter)
    End Sub

    '// Check if player cna block an incoming attack
    Public Function CanBlock(enemyPosition As COORD3Sng) As Boolean
        '// Point character is pointing at, unit distance away
        Dim frontVector As New COORD3Sng(CSng(Math.Sin(rotation) + location.x), 0, CSng(Math.Cos(rotation) + location.z))
        Dim enemyVector As New COORD3Sng(enemyPosition.x, 0, enemyPosition.z)
        Dim positionVector As New COORD3Sng(location.x, 0, location.z)
        '// Check angle between attack and block direction
        Dim angle As Single = Theta(positionVector, frontVector, enemyVector)
        '// Can block if pointing at enemy and holding shield up
        Return angle < 1.3 And animationName = "block"
    End Function

    '// Get chance that a shield can block an attack
    Public Shared Function ShieldChance(shieldLevel As Integer, shieldPower As Integer) As Single
        Dim chance As Single = 1 - CSng(Math.Max(0.3 / (shieldPower * (1 + shieldLevel * 0.2F)), 0))
        If chance < 0 Then
            '// Clamp chance to 0
            Return 0
        End If
        Return chance
    End Function

    '// Get details to be displayed in the character form
    Public Function GetStats() As String
        Return "Max Health<Newline>" & maxHealth & "<Newline>Max Stamina<Newline>" & maxStamina & "<Newline>Attack Power<Newline>" & Math.Floor(BASE_DAMAGE + weapon.baseData.power * (1 + 0.2 * weapon.specificData.level))
    End Function

    Public Function GetItems() As String
        Return "Weapon<Newline>" & weapon.baseData.name & "<Newline>Shield<Newline>" & shield.baseData.name
    End Function

    '// Change camera view to respond to mouse movement
    Public Sub MouseMovement(deltaT As Single, mouseChange As MouseInput.POINT)
        '// Clockwise from north
        cameraRotation += mouseChange.x * 0.01F
        cameraElevation += mouseChange.y * 0.01F
        '// Clamp elevation values
        If cameraElevation < -Math.PI / 2 Then cameraElevation = -Math.PI / 2
        If cameraElevation > Math.PI / 2 Then cameraElevation = Math.PI / 2
        If cameraRotation < 0 Then cameraRotation += CSng(Math.PI * 2)
        If cameraRotation > Math.PI * 2 Then cameraRotation -= CSng(Math.PI * 2)
        If firstPerson Then
            '// Make player model point in the same direction
            rotation = cameraRotation
        End If
    End Sub

    '// Deal damage to mobs and perform the attack
    '// Called once during the attack animation
    Private Sub Attack(mobs As Mob())
        Dim successfulAttack As Boolean = False
        Dim attackInstanceSuccess As Boolean
        For i = 0 To mobs.Length - 1
            attackInstanceSuccess = False
            '// Check if a mob can be hit
            If mobs(i).InRange(location, cameraRotation, ATTACK_RANGE) Then
                Select Case weapon.baseData.attackType
                    Case Inventory.DamageType.Splash
                        '// Splash attack deals damage to all mobs in range
                        attackInstanceSuccess = True
                    Case Inventory.DamageType.Focused
                        '// Focused attack deals damage to the closest target
                        If targetFocusIndex = i Then
                            attackInstanceSuccess = True
                        End If
                End Select
                '// Perform on mob
                If attackInstanceSuccess Then
                    If animationName = "slasha" Then
                        '// Deal extra damage for jump attack
                        mobs(i).TakeDamage(CInt((BASE_DAMAGE + weapon.baseData.power * (1 + 0.1 * weapon.specificData.level)) * 1.5))
                    Else
                        '// Increase damage with each combo
                        mobs(i).TakeDamage(CInt((BASE_DAMAGE + weapon.baseData.power) * (1 + 0.1 * weapon.specificData.level) * (1 + 0.2F * attackAnimationIndex)))
                    End If
                    '// Ensure a single sound is played
                    successfulAttack = True
                End If
            End If
        Next
        '// Play attack sound
        If successfulAttack Then
            audio.PlaySound(soundAttack, False, True, location, 1)
        End If
    End Sub

    '// When an enemy is killed, update mission progress
    Public Sub KilledEnemy(mobType As Integer)
        missionList.KilledEnemy(mobType)
    End Sub

    '// Handle mouse input
    Public Sub MouseClicks(deltaT As Single, mobs As Mob(), NPCs As NPCManager)
        '// Get current targeted enemy
        Dim targetFocus As Mob
        If targetFocusIndex < mobs.Length Then
            targetFocus = mobs(targetFocusIndex)
        End If
        If actionInProgress Then
            '// Already in an attack, so limit number of actions that can be made
            hasHit = True

            '// Once per attack, check for damage dealt
            If Not hasAttacked And animationProgress > model.animations.GetAnimationFrame(animationName).attack / 20 Then
                hasAttacked = True
                Attack(mobs)
            End If

            '// Perform combo attacks
            If MouseInput.MouseClick(MouseInput.Buttons.Left) And Not animationName = "slasha" And Not animationName = "blockbreak" Then
                If animationProgress < (model.animations.GetAnimationFrame(animationName).attack + 10) / 20 And hasAttacked Then
                    '// Use next combo animation
                    attackAnimationIndex += 1
                    If attackAnimationIndex < weapon.baseData.attackAnimation.Length Then
                        '// Set animation
                        actionInProgress = False
                        animationName = weapon.baseData.attackAnimation(attackAnimationIndex)
                        hasAttacked = False
                        animationProgress = 0
                        animationList.Clear()
                        actionInProgress = True
                    End If
                End If
            End If

            '// Finish attack and revert to idle animation
            If animationProgress + deltaT * 2 > model.animations.GetAnimationFrame(animationName).data.Length / 20 Then
                actionInProgress = False
                animationProgress = 0
                animationName = "idle"
                animationList.Clear()
            End If

        Else
            If MouseInput.MouseClick(MouseInput.Buttons.Left) And stamina > 10 Then
                '// Perform an attack
                animationName = weapon.baseData.attackAnimation(0)
                If weapon.baseData.name = "Sword" And (Not closeToFloor Or (Not onFloor And yVelocity > 0)) Then
                    '// Jump attack when in the air and holding a sword
                    animationName = weapon.baseData.attackAnimation(0) & "a"
                End If
                attackAnimationIndex = 0
                actionInProgress = True
                hasAttacked = False
                animationProgress = 0
                stamina -= 10

                '// Attack in direction camera is facing
                rotation = cameraRotation
                If targetLock And Not IsNothing(targetFocus) Then
                    '// Point at target if target lock is on
                    rotation = CSng(Math.Atan2(targetFocus.location.x - location.x, targetFocus.location.z - location.z))
                End If
            ElseIf MouseInput.MouseDown(MouseInput.Buttons.Right) Then
                If Not Talk(NPCs) Then
                    '// Block attack if not in range of an NPC
                    Block(deltaT)
                End If
            Else
                '// No mouse input
                If animationName = "block" Then
                    '// Revert from block animation to idle animation
                    animationName = "idle"
                End If
                If animationName = "blockbreak" And animationProgress + deltaT * 2 > model.animations.GetAnimationFrame(animationName).data.Length / 20 Then
                    '// Play idle animation if not playing knockback animation
                    animationName = "idle"
                End If
            End If
        End If
    End Sub

    '// Check if an NPC is inrange to be talked to
    Private Function Talk(NPCs As NPCManager) As Boolean
        Dim closestNPC As Integer = NPCs.GetClosestNPC()
        If closestNPC > -1 Then
            talkTarget = closestNPC
            Return True
        Else
            Return False
        End If
    End Function

    '// Handle shield operation
    Private Sub Block(deltaT As Single)
        '// Cannot block unless holding a shield
        If shield.baseData.name <> "No Item" Then
            '// Cannot block when dodging, jumping, or being knocked back
            If animationName <> "dodge" And animationName <> "jump" And animationName <> "blockbreak" Then
                If animationName <> "block" Then
                    '// Start block animation
                    animationProgress = 0
                End If
                animationName = "block"
                animationList.Clear()
                animationProgress += deltaT
                '// Clamp animation progress to block position
                If animationProgress > 0.5 Then animationProgress = 0.5
            End If
            '// Recover from knockback and block
            If animationName = "blockbreak" And animationProgress + deltaT * 2 > model.animations.GetAnimationFrame(animationName).data.Length / 20 Then
                animationProgress = 0.5
                actionInProgress = False
                animationName = "block"
                animationList.Clear()
            End If
        End If
    End Sub

    '// Check for key press to access inventory
    Public Sub AccessInventory(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer)
        If KeyboardInput.KeyDown("E"c) Then
            Menu.ChangeForm(formArray, currentForm, Menu.Controls.ID_frmInventory, 0)
        End If
    End Sub

    '// Heal over time
    Public Sub RegenerateHealth(deltaT As Single)
        If health = maxHealth Then
            '// Prevent health extending beyond the maximum
            timeSinceHeal = 0
        Else
            timeSinceHeal += deltaT
            '// Heal 1 health per time specified in time to heal
            If timeSinceHeal > TIME_TO_HEAL Then
                timeSinceHeal = 0
                health += 1
            End If
        End If
    End Sub

    '// Handle keyboard input to control the character
    Public Sub KeyboardMovement(deltaT As Single)
        Dim speedMultiplier As Single = 1
        Dim deltaX As Single
        Dim deltaZ As Single
        Dim normX As Single
        Dim normZ As Single
        Dim directionX As Single
        Dim directionZ As Single
        Dim currentAnimation As String

        currentAnimation = animationName
        locationOffset = New COORD3Sng(0, 0, 0)

        deltaX = 0
        deltaZ = 0
        '// Move character in response to WASD or equivalent
        If Not animationName = "block" And Not animationName = "blockbreak" Then
            If KeyboardInput.KeyDown(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.MoveForward)) Then
                deltaZ += 1
            End If
            If KeyboardInput.KeyDown(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.MoveBack)) Then
                deltaZ -= 1
            End If
            If KeyboardInput.KeyDown(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.MoveLeft)) Then
                deltaX -= 1
            End If
            If KeyboardInput.KeyDown(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.MoveRight)) Then
                deltaX += 1
            End If
        End If

        '// Perform dodge roll
        If KeyboardInput.KeyDown(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.Dodge)) And Not actionInProgress And Not dodge And Not animationName = "block" And Not animationName = "blockbreak" Then
            animationProgress = 0
            dodge = True
        End If

        recoveringStamina = True
        bypassAnimationTranslation = False

        If Not dodge Then
            If deltaX = 0 And deltaZ = 0 Then
                '// If there is no movement, play idle animation
                If Not actionInProgress Then
                    currentAnimation = "idle"
                End If
            Else
                If Not actionInProgress Then
                    '// Switch mode to walk or running if not attacking
                    If KeyboardInput.KeyDown(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.Sprint)) And ((stamina > 0 And Not recoveringStamina) Or stamina > 20) Then
                        speedMultiplier = 2
                        currentAnimation = "run"
                        recoveringStamina = False
                    Else
                        currentAnimation = "walk"
                    End If
                    bypassAnimationTranslation = True
                End If
                If Not actionInProgress Or Not targetLock Then
                    '// Point in the direction the player is moving
                    rotation = CSng(rotation Mod (Math.PI * 2))
                    cameraRotation = CSng(cameraRotation Mod (Math.PI * 2))
                    '// Smooth rotation to target
                    rotation = CSng(cameraRotation + Math.Atan2(deltaX, deltaZ))
                End If
            End If
        End If

        '// Recover or lose stamina
        If recoveringStamina Then
            stamina += deltaT * 10
        Else
            stamina -= deltaT * 5
        End If

        '// Normalise direction vectors
        normX = CSng(deltaX / Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ + 0.01))
        normZ = CSng(deltaZ / Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ + 0.01))

        deltaX = normX
        deltaZ = normZ

        '// Play dodge animation and move if necessary
        If dodge Then
            PerformDodge(deltaT, currentAnimation)
        End If

        If targetLock And actionInProgress Then
            '// Move towards target
            directionX = CSng(deltaX * Math.Cos(rotation) + deltaZ * Math.Sin(rotation))
            directionZ = CSng(deltaZ * Math.Cos(rotation) - deltaX * Math.Sin(rotation))
        Else
            '// Move in the direction the camera is facing
            directionX = CSng(deltaX * Math.Cos(cameraRotation) + deltaZ * Math.Sin(cameraRotation))
            directionZ = CSng(deltaZ * Math.Cos(cameraRotation) - deltaX * Math.Sin(cameraRotation))
        End If

        '// Apply gravity
        If onFloor Then
            yVelocity = 0
        Else
            yVelocity -= deltaT * 9.81F
        End If

        '// Jump if on the floor
        If KeyboardInput.KeyDown(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.Jump)) And location.y - floorHeight < 0.2 And stamina > 20 Then
            yVelocity = 4
            stamina -= 10
            animationProgress = 0
        End If
        location.y += yVelocity * deltaT

        '// Allow the character to fly, used for testing
        If KeyboardInput.KeyDown(38) Then
            location.y += deltaT * BASE_SPEED
        End If
        If KeyboardInput.KeyDown(40) Then
            location.y -= deltaT * BASE_SPEED
        End If

        '// Clamp stamina values
        If stamina < 0 Then stamina = 0
        If stamina > maxStamina Then stamina = maxStamina

        If Not dodge And Not actionInProgress Then
            If Not onFloor And yVelocity > 0 Then
                '// Continue jump animation if moving upwards
                currentAnimation = "jump"
                animationProgress = (location.y - floorHeight) * 2
                If animationProgress > 0.5 Then
                    '// Clamp to in-progress jump position
                    animationProgress = 0.5
                End If
            End If
            If Not closeToFloor And yVelocity < 0 Then
                '// Play jump landing animation when falling
                currentAnimation = "jump"
                animationProgress = 1 - (location.y - floorHeight) * 2
                If animationProgress < 0.5 Then
                    '// Clamp to in-progress jump position
                    animationProgress = 0.5
                End If
            End If
        End If

        '// Apply animation if not overridden by shield animations
        If animationName <> "block" And animationName <> "blockbreak" Then
            animationName = currentAnimation
        End If

        '// Ignore translations from movement animations
        For i = 0 To animationList.Count - 1
            If animationList(i).name = "walk" Or animationList(i).name = "run" Then
                bypassAnimationTranslation = True
            End If
        Next

        '// Move player if walking or running
        If bypassAnimationTranslation Then
            location.x += directionX * deltaT * BASE_SPEED * speedMultiplier
            location.z += directionZ * deltaT * BASE_SPEED * speedMultiplier
        End If

    End Sub

    '// Set the dodge roll animation
    Private Sub PerformDodge(deltaT As Single, ByRef currentAnimation As String)
        Dim dz As Single = CSng(Math.Cos(rotation))
        Dim dx As Single = CSng(Math.Sin(rotation))
        currentAnimation = "dodge"
        If animationProgress > 1 Then
            dodge = False
            currentAnimation = "idle"
        End If
    End Sub
End Class

