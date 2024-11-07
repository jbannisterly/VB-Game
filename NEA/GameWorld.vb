Option Strict On
#Const DEBUG = False
#Const RAIN_DEBUG = False
#Const TIME_DEBUG = True

Imports System.Console
Imports System.Runtime.InteropServices
Imports NEA.CoordDataTypes
Imports NEA.OpenGLImporter
'// Handles overall functionality of the program
Public Class GameWorld
    '// Variables required throughout the program
    Public Shared frmArray As GUIObject.Form()
    Public Shared saveFile As String
    Public Shared character As Player
    Public Shared deathProgress As Single
    Public Shared gamesToLoad As SaveGame.LoadGameItem()
    Public Shared gameToLoadID As Integer
    Public Shared randoms As Single()
    Public Shared currentDisplayMode As DisplayMode
    Public Shared isRaining As Boolean = False
    Public Shared respawnFlag As Boolean = False
    Public Shared timeOfDay As Single
    Public Shared activeForm As Integer
    Public Shared NPCs As NPCManager

    '// Speed up or slow down, used for testing animations
    Public Const TIME_RATIO As Single = 1
    Const RAIN_DURATION As Single = 60

    '// Create new form with health bars
    Private Shared Function GetGameplayForm(healthController As Health) As GUIObject.Form
        Dim gameplayForm As New GUIObject.Form
        gameplayForm.NewChild(healthController.enemyHealth)
        gameplayForm.NewChild(healthController.healthNumeric)
        gameplayForm.NewChild(healthController.staminaNumeric)
        gameplayForm.NewChild(healthController.healthBar)
        gameplayForm.NewChild(healthController.staminaBar)
        Return gameplayForm
    End Function

    '// Initialise references to the OpenGL context class
    Private Shared Sub InitialiseContextBinding(ByRef context As OpenGLContext)
        Sky.context = context
        GrassRenderer.context = context
        TerrainRenderer.context = context
        TextureLoader.context = context
    End Sub

    '// Main game subroutine
    Public Shared Sub MainGame(forms As GUIObject.Form(), formInterface As GUIInstance, ByRef keyBindings As KeyboardInput.KeyBinding, ByRef graphicsBindings As GraphicsSettings, ByRef context As OpenGLContext, ByRef audio As AudioManager)
#Region "Initialisation"
        frmArray = forms
        If IO.File.Exists("Debug.txt") Then IO.File.Delete("Debug.txt")

        '// Create health bars
        Dim healthController As New Health(Menu.GetLabelStyle())
        Dim gameplayForm As GUIObject.Form = GetGameplayForm(healthController)
        InitialiseContextBinding(context)

        Dim newActiveForm As Integer

        '// Declare programs, models and terrain
        Dim lightSource As New COORD3Sng
        Dim GLTFProgram As UInt32 = OpenGLWrapper.CreateProgram(context, "GLTF", Shaders.VERTEX_SHADER_ANIMATED_MODEL, Shaders.FRAGMENT_SHADER_GLTF_DEFER)
        Dim GLTFProgramSimple As UInt32 = OpenGLWrapper.CreateProgram(context, "GLTF Simple", Shaders.VERTEX_SHADER_ANIMATED_MODEL, Shaders.FRAGMENT_SHADER_GLTF)
        Dim models As New ModelManager(context)
        models.LoadModels("Resources\Models", CInt(GLTFProgram))
        Dim blankModel As New GLTFModel("NoItem", CInt(GLTFProgram), context)
        character = New Player(models.GetModel("PlayerModel2"), forms(Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmInventory)), forms(Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmCrafting)), forms(Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmMissions)), audio)
        Dim shadowRenderer As Shadow
        Dim worldRenderer As RenderWorld
        Dim deathStart As Double
        Dim weapon As New Mob(blankModel, audio)
        Dim shield As New Mob(blankModel, audio)
        randoms = PerlinNoise.GenerateRandomVectors(128 * 128, 1, PerlinNoise.VectorRange.ZeroToOne)
        Dim randomPtr As IntPtr = Marshal.AllocHGlobal(128 * 128 * 4 * 2)
        Marshal.Copy(randoms, 0, randomPtr, randoms.Length)

        Dim enemies As New EnemyManager(character, CInt(GLTFProgram), randoms, context, audio, models)
        NPCs = New NPCManager(models, audio, character, forms, enemies)

        character.AddChild(weapon, "ItemR")
        character.AddChild(shield, "ShieldL")
        character.keyBindings = keyBindings

        Dim terrainGenerator As New TerrainGenerator(randomPtr, context)

        Dim vertLst As New List(Of Single)
        Dim vertArr(CHUNK_SIZE * CHUNK_SIZE * 3 - 1) As Single
        Dim indexArr((CHUNK_SIZE - 1) * (CHUNK_SIZE - 1) * 6 - 1) As Integer
        Dim vertArrShadowMap(SHADOW_MAP_SIZE * SHADOW_MAP_SIZE * 3 - 1) As Single
        Dim indexArrShadowMap((SHADOW_MAP_SIZE - 1) * (SHADOW_MAP_SIZE - 1) * 6 - 1) As Integer
        Dim monitorSize As COORD2Short = Window.GetSize()
        Dim normal(2) As Single
        Dim shadowFocusPoint As COORD2Sng
        Dim shadowTexturesStatic As UInt32()
        Dim shadowTexturesDynamic As UInt32()
        Dim skyVAO As UInt32
        Dim terrainVAO As UInt32
        Dim skyProgram As UInt32
        Dim shadowVAO As UInt32
        Dim grassVAO As UInt32
        Dim healthVAO As UInt32
        Dim ripples As New RippleController()

        '// Initialise terrain renderer
        TerrainRenderer.GridVerticesIndices(vertArr, indexArr, CHUNK_SIZE)
        TerrainRenderer.GridVerticesIndices(vertArrShadowMap, indexArrShadowMap, SHADOW_MAP_SIZE)

        '// Declare programs and buffers
        Dim bufferPtr As IntPtr = Marshal.AllocHGlobal(1024)
        Dim texPtr As IntPtr = Marshal.AllocHGlobal(4)
        Dim framebufferPtr As IntPtr = Marshal.AllocHGlobal(1024)
        Dim attributeArrayPtr As IntPtr = Marshal.AllocHGlobal(1024)
        Dim terrainProgram As UInt32
        Dim grassProgram As UInt32
        Dim rainProgram As UInt32
        Dim shadowTerrainProgram As UInt32
        Dim shadowModelProgram As UInt32
        Dim deferredProgram(1) As UInt32
        Dim healthBarProgram As UInt32
        Dim chunkBuffer As UInt32
        Dim gridVertexBuffer As UInt32
        Dim textureGrass As UInt32
        Dim grassVerticesArr(GRASS_DISTANCE * GRASS_DISTANCE * 16 * 3 - 1) As Single
        Dim grassVertex As UInt32
        Dim healthVerticesArr(1000) As Single
        Dim healthVertex As UInt32
        Dim healthColourArr(1000) As Single
        Dim healthColour As UInt32

        '// Initialise sky renderer
        skyVAO = Sky.GenerateVAO()
        skyProgram = OpenGLWrapper.CreateProgram(context, "sky", Shaders.VERTEX_SHADER_SKY, Shaders.FRAGMENT_SHADER_SKY)
        Sky.LoadOutputTexture()

        '// Initialise buffers
        glGenVertexArrays(4, attributeArrayPtr)
        terrainVAO = CUInt(Marshal.ReadInt32(attributeArrayPtr))
        shadowVAO = CUInt(Marshal.ReadInt32(attributeArrayPtr + 4))
        grassVAO = CUInt(Marshal.ReadInt32(attributeArrayPtr + 8))
        healthVAO = CUInt(Marshal.ReadInt32(attributeArrayPtr + 12))
        '// Initialise programs
        deferredProgram(0) = OpenGLWrapper.CreateProgram(context, "deferred", Shaders.VERTEX_SHADER_DEFERRED, Shaders.FRAGMENT_SHADER_DEFERRED.Split("@"c)(0) & "1.0" & Shaders.FRAGMENT_SHADER_DEFERRED.Split("@"c)(1))
        deferredProgram(1) = OpenGLWrapper.CreateProgram(context, "deferred", Shaders.VERTEX_SHADER_DEFERRED, Shaders.FRAGMENT_SHADER_DEFERRED.Split("@"c)(0) & "ShadowMask()" & Shaders.FRAGMENT_SHADER_DEFERRED.Split("@"c)(1))

        '// Initialise shadow map creator
        glGenFramebuffers(1, framebufferPtr)
        glBindFramebuffer(GL_FRAMEBUFFER, CUInt(Marshal.ReadInt32(framebufferPtr)))

        shadowModelProgram = OpenGLWrapper.CreateProgram(context, "shadowModel", Shaders.VERTEX_SHADER_SHADOW_ANIMATION, Shaders.FRAGMENT_SHADER_SHADOW)
        shadowTerrainProgram = OpenGLWrapper.CreateProgram(context, "shadowTerrain", Shaders.VERTEX_SHADER_SHADOW_TERRAIN, Shaders.FRAGMENT_SHADER_SHADOW)
        shadowRenderer = New Shadow(monitorSize, {16, 64, 256}, shadowVAO, vertArrShadowMap, indexArrShadowMap, context)
        shadowTexturesStatic = shadowRenderer.GenerateShadowTextures(3)
        shadowTexturesDynamic = shadowRenderer.GenerateShadowTextures(3)

        '// Initialise health bar display
        context.glBindVertexArray(healthVAO)
        glBindFramebuffer(GL_FRAMEBUFFER, 0)
        healthBarProgram = OpenGLWrapper.CreateProgram(context, "health", Shaders.VERTEX_SHADER_HEALTH, Shaders.FRAGMENT_SHADER_HEALTH)
        glEnableVertexAttribArray(0)
        glEnableVertexAttribArray(1)
        glGenBuffers(2, bufferPtr)
        healthVertex = CUInt(Marshal.ReadInt32(bufferPtr))
        healthColour = CUInt(Marshal.ReadInt32(bufferPtr + 4))
        OpenGLWrapper.BindBufferToProgramAttributes(healthVertex, 2, GL_FLOAT, 0, OpenGLWrapper.VertexType.FLOAT)
        OpenGLWrapper.BindBufferToProgramAttributes(healthColour, 3, GL_FLOAT, 1, OpenGLWrapper.VertexType.FLOAT)

        context.glUseProgram(shadowTerrainProgram)
        context.glBindVertexArray(terrainVAO)
        glBindFramebuffer(GL_FRAMEBUFFER, CUInt(Marshal.ReadInt32(framebufferPtr)))
        context.glBindTexture(GL_TEXTURE_0 + 3, terrainGenerator.textureID)
        Dim uniformLocation As Integer = glGetUniformLocationStr(CInt(shadowTerrainProgram), "heightmap")
        glUniform1i(uniformLocation, 3)

        '// Intialise terrain renderer with shadow maps
        TerrainRenderer.InitialiseBuffers(bufferPtr, vertArr, indexArr, gridVertexBuffer, chunkBuffer)
        glGenBuffers(1, bufferPtr)
        grassVertex = CUInt(Marshal.ReadInt32(bufferPtr))
        TerrainRenderer.GridVertices(grassVerticesArr, GRASS_DISTANCE * 4)
        OpenGLWrapper.BufferData(grassVerticesArr, GL_ARRAY_BUFFER, grassVertex)

        terrainProgram = OpenGLWrapper.CreateProgram(context, "terrain", Shaders.VERTEX_SHADER_TERRAIN, Shaders.FRAGMENT_SHADER_TERRAIN_DEFER)
        glBindFramebuffer(GL_FRAMEBUFFER, 0)
        glGenTextures(2, texPtr)
        TerrainRenderer.BindTiledTextures(CInt(terrainProgram), CUInt(Marshal.ReadInt32(texPtr)))
        TerrainRenderer.BindHeightMap(shadowTexturesDynamic, terrainGenerator, CInt(terrainProgram))
        TerrainRenderer.BindShadowMap(shadowTexturesDynamic, CInt(deferredProgram(0)))
        TerrainRenderer.BindShadowMap(shadowTexturesDynamic, CInt(deferredProgram(1)))

        '// Initialise grass and sky programs and bind textures
        textureGrass = CUInt(Marshal.ReadInt32(texPtr + 4))
        TextureLoader.LoadTextureCubeMap("CubeTest2", CUInt(Marshal.ReadInt32(texPtr)), GL_LINEAR, GL_LINEAR)
        glActiveTexture(GL_TEXTURE_0 + 2)
        glEnable(GL_TEXTURE_2D)
        glBindTexture(GL_TEXTURE_CUBE_MAP, CUInt(Marshal.ReadInt32(texPtr)))
        glUniform1i(glGetUniformLocationStr(CInt(terrainProgram), "sky"), 2)

        grassProgram = OpenGLWrapper.CreateProgram(context, "grass", Shaders.VERTEX_SHADER_GRASS, Shaders.FRAGMENT_SHADER_GRASS, Shaders.GEOMETRY_SHADER_GRASS)
        TextureLoader.LoadTexture2D("grass3D", textureGrass, GL_NEAREST_MIPMAP_NEAREST, GL_NEAREST, True)
        GrassRenderer.InitialiseGrass(grassProgram, grassVAO, grassVertex, terrainGenerator.textureID, terrainGenerator.normalTextureID)

        '// Timing variable declarations
        Dim deltaT As Single
        Dim totT As Single
        Dim test As Integer
        Dim stopwatchInstance As New Stopwatch
        Dim timeToWeatherChange As Single = 120

        Dim frameCount As Integer
        Dim times(5) As Single
        Dim dMouse As MouseInput.POINT
        Dim lastSave As Double = Timer
        Dim keysPressed(127) As Boolean
        Dim mainMenu As Boolean
        Dim prevMainMenu As Boolean = False
        Dim terrainView As COORD2Sng = New COORD2Sng(0, 0)
        Dim enemyFocusIndex As Integer
        Dim matrixRelative As New Matrices(4, 4, True)
        Dim matrixView As New Matrices(4, 4, True)
        Dim matrixPerspective As New Matrices(4, 4, True)

        '// Camera variable declarations
        Dim cameraPosition As New COORD3Sng
        Dim cameraRotation As Single
        Dim cameraElevation As Single

        '// Initialise audio
        Dim soundMusic As Integer = audio.LoadResource("Theme.wav")
        Dim soundRain As Integer = audio.LoadResource("Rain.wav")
        Dim soundRainInstance As Integer
        audio.PlaySound(soundMusic, True, False, New COORD3Sng(0, 0, 0), 0.3)

        TimeStamp.Init()

        '// Initialise overall render program
        worldRenderer = New RenderWorld(monitorSize, terrainProgram, skyProgram, GLTFProgram, deferredProgram, context)
        worldRenderer.graphicsBindings = graphicsBindings
        Sky.texPtr = texPtr

        '// Initialise character menu
        Dim renderTarget As GUIObject.RenderTarget
        glBindFramebuffer(GL_FRAMEBUFFER, 0)
        forms(Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmCharacter)).GetElementByID(Menu.Controls.ID_renCharacter, renderTarget)
        renderTarget.model = character
        renderTarget.GLTFProgram = GLTFProgramSimple

        character.InitialiseReferences(GLTFProgramSimple, enemies)

        RegenerateTerrain(terrainView, terrainProgram, grassProgram, shadowTerrainProgram, terrainGenerator, monitorSize, context)

        Clear()
#End Region
        '// Main game loop

        Do
            CursorVisible = False
            currentDisplayMode = GetDisplayMode(activeForm, forms)
            mainMenu = currentDisplayMode = DisplayMode.MainMenu
            ControlCamera(cameraPosition, cameraRotation, cameraElevation, GetFormID(forms, activeForm), NPCs, mainMenu, prevMainMenu)

            If currentDisplayMode = DisplayMode.Dialogue Then
                Talk(cameraPosition, cameraRotation, cameraElevation, NPCs, deltaT, activeForm)
            Else
                NPCs.ClearDialogue()
            End If

            '// Set audio reference to camera
            audio.listenerLoc = cameraPosition
            audio.listenerRot = cameraRotation
            newActiveForm = activeForm

            '// Regenerate terrain if the player has moved too far from the centre
            '// Allows texture memory to be reduced
            If Math.Abs(terrainView.x + 500 - cameraPosition.x) > 100 Or Math.Abs(terrainView.y + 500 - cameraPosition.z) > 100 Then
                terrainView.x = cameraPosition.x - 500
                terrainView.y = cameraPosition.z - 500
                RegenerateTerrain(terrainView, terrainProgram, grassProgram, shadowTerrainProgram, terrainGenerator, monitorSize, context)
            End If

            '// Meaure elapsed time and control the day/night cycle
#Region "Time"
            TimeStamp.Stamp(times(0))
            frameCount += 1
            stopwatchInstance.Stop()
            deltaT = CSng(stopwatchInstance.ElapsedTicks / Stopwatch.Frequency * TIME_RATIO)
            If activeForm > -1 Then totT = 0 : frameCount = -1
            timeOfDay += CSng(deltaT * Math.PI * 2 / LENGTH_OF_DAY)
            If timeOfDay > Math.PI * 2 Then timeOfDay -= CSng(Math.PI) * 2
            totT += deltaT
            If activeForm = -1 Then
                timeToWeatherChange -= deltaT
            End If
            stopwatchInstance.Reset()
            stopwatchInstance.Start()
            If currentDisplayMode = DisplayMode.MainMenu Then
                timeOfDay = 0
            End If
#End Region
#Region "Time Debug"
            '// Output fps
#If TIME_DEBUG Then
            If frameCount Mod 100 = 0 Then
                IO.File.AppendAllText("Performance.txt", Math.Round(totT, 2) * 10 & vbNewLine)
                'For i = 0 To 5
                '    Debug.Write(Math.Round(times(i), 2) * 10 & " ")
                'Next
            End If
#End If
#End Region
            '// Get mouse input
            MouseInput.NextFrame()
            If currentDisplayMode = DisplayMode.Inventory And KeyboardInput.KeyDown(27) Then
                activeForm = -1
            End If
            '// Change camera view
            character.FirstPersonCheck(currentDisplayMode, prevMainMenu)

            '// Hide the load game button if there are no games to load
            If mainMenu And Not prevMainMenu Then
                Dim loadGame As GUIObject.Button
                forms(Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmMain)).GetElementByID(Menu.Controls.ID_btnLoadGame, loadGame)
                If Not LoadGameExists() Then
                    loadGame.visible = False
                Else
                    loadGame.visible = True
                End If
            End If

            '// Refresh inventory
            If character.inventoryItems.inventoryDisplay.pageInvalid Then
                character.inventoryItems.SetPage(character.inventoryItems.inventoryDisplay.currentPage)
            End If

            prevMainMenu = mainMenu

#Region "Autosave"
            '// Save every 5 seconds when in-game
            If Timer - lastSave > 5 And activeForm = -1 Then
                SaveGame.SaveGame(saveFile, character, timeOfDay, NPCs)
                lastSave = Timer
            End If
#End Region
#Region "Animations"
            '// Remove animations that finished a long time ago
            character.CleanUpAnimations()
            '// Update all animations
            If currentDisplayMode = DisplayMode.MainGame Or currentDisplayMode = DisplayMode.Dialogue Then
                For i = 0 To enemies.enemies.Count - 1
                    enemies.enemies(i).UpdateMatrices()
                Next
                For i = 0 To NPCs.NPCList.Count - 1
                    NPCs.NPCList(i).UpdateMatrices()
                Next
                character.UpdateMatrices()
            End If
#End Region
#Region "Shadow map"
            '// Generate shadows if enabled and world is visible
            If currentDisplayMode <> DisplayMode.Inventory And graphicsBindings.GetGraphicsSettings(GraphicsSettings.SettingsIndex.Shadow) = 1 Then
                '// Update static shadows from terrain every 5 frames
                If frameCount Mod 5 < 1 Then
                    lightSource = New COORD3Sng(CSng(Math.Sin(timeOfDay)), CSng(Math.Cos(timeOfDay)), 0)
                    '// Select between sun and moon as main light source
                    If timeOfDay > Math.PI / 2 And timeOfDay < 3 * Math.PI / 2 Then
                        lightSource.x *= -1
                        lightSource.y *= -1
                    End If
                    glEnable(GL_CULL_FACE)
                    glCullFace(GL_FRONT)
                    shadowRenderer.SetFocusPoint(cameraPosition, timeOfDay)
                    shadowRenderer.lightGradient = lightSource.x / lightSource.y
                    context.glBindVertexArray(terrainVAO)
                    shadowRenderer.InitialiseShadowProgram(CInt(shadowTerrainProgram))
                    shadowFocusPoint = shadowRenderer.ProjectTerrainShadows(shadowTexturesStatic, indexArrShadowMap.Length, shadowTerrainProgram, CUInt(Marshal.ReadInt32(framebufferPtr)))
                End If
                '// Update dynamic shadows from entities every frame
                If frameCount Mod 1 < 1 And Not mainMenu Then
                    glEnable(GL_CULL_FACE)
                    glCullFace(GL_BACK)
                    '// Draw shadow map over terrain shadow map
                    shadowRenderer.CopyShadows(shadowTexturesStatic, shadowTexturesDynamic)
                    shadowRenderer.InitialiseShadowProgram(CInt(shadowModelProgram))
                    enemies.CastShadows(shadowRenderer, CInt(shadowModelProgram), CUInt(Marshal.ReadInt32(framebufferPtr)), shadowTexturesDynamic)
                    NPCs.CastShadows(shadowRenderer, CInt(shadowModelProgram), CUInt(Marshal.ReadInt32(framebufferPtr)), shadowTexturesDynamic)
                    shadowRenderer.ProjectModelShadows(shadowTexturesDynamic, CInt(shadowModelProgram), CUInt(Marshal.ReadInt32(framebufferPtr)), character, False, {New Matrices(4, 4, True)}, -1)
                    shadowRenderer.ResetTarget()
                    '// Force shadow map to finish before using it
                    glFinish()
                End If
            End If
#End Region

            TimeStamp.Stamp(times(1))

            dMouse = MouseInput.DeltaMouse()

            '// Handle user input for the character
#Region "Control Character"
            If currentDisplayMode = DisplayMode.MainGame Then
                ControlCharacter(character, deltaT, dMouse, randoms, enemies.enemies.ToArray(), NPCs, character.health <= 0)
                character.AccessInventory(forms, activeForm)
            End If
            If character.talkTarget <> -1 Then
                activeForm = Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmTalk)
            End If

            '// Used for debugging
#If CANFLY Then
            If KeyboardInput.KeyDown(38) Then character.location.y += deltaT * 10
            If KeyboardInput.KeyDown(40) Then character.location.y -= deltaT * 10
#Else
            '// Apply gravity
            character.floorHeight = PerlinNoise.GetHeight(character.location.x, character.location.z, randoms) * 100
            character.onFloor = character.floorHeight >= character.location.y
            character.closeToFloor = character.floorHeight + 0.5 >= character.location.y
            If character.onFloor Then
                character.location.y = character.floorHeight
            End If
#End If
            If currentDisplayMode = DisplayMode.MainMenu Then
                '// Reset character statistics 
                character.health = 100
                deathProgress = 0
                deathStart = 0
                '// Rotate camera to showcase terrain
                cameraRotation += deltaT * 0.05F
                cameraPosition.y = PerlinNoise.GetHeight(cameraPosition.x, cameraPosition.z, randoms) * 100 + 10
            End If
            If currentDisplayMode = DisplayMode.MainGame Then
                '// If underwater, then drown
                If character.location.y < 38.5 Then
                    character.health = -1
                End If
                '// Death check
                If character.health <= 0 Then
                    '// Play death animation and fade to grey
                    If deathStart = 0 Then
                        '// Initialise death animation
                        deathStart = Timer
                        character.animationName = "death"
                        character.animationProgress = 0
                        character.animationList.Clear()
                    End If
                    deathProgress = CSng(Timer - deathStart) * 0.2F
                    If deathProgress > 1 Then deathProgress = 1
                Else
                    '// Reset death animation if not dead
                    deathProgress = 0
                    deathStart = 0
                End If
                '// Create a ripple when in water
                If ripples.CanAddRipple() Then
                    ripples.AddRipple(character.location)
                End If
            End If
#End Region
#Region "Render World"
            '// Only display icon when in main game
            character.iconVisible = activeForm = -1
            If currentDisplayMode = DisplayMode.Inventory Then
                '// CLear to black
                glClearColor(0, 0, 0, 0)
                glClear(GL_COLOUR_BUFFER_BIT)
            Else
                If graphicsBindings.GetGraphicsSettings(GraphicsSettings.SettingsIndex.Reflection) = 1 Then
                    '// Render world upside down for water reflections
                    worldRenderer.RenderWorld(character, monitorSize, enemies, NPCs, lightSource, shadowFocusPoint, chunkBuffer, terrainVAO, grassProgram, grassVAO, textureGrass, totT, timeOfDay, True, ripples, 1 - deathProgress, Not mainMenu, isRaining, rainProgram, cameraPosition, cameraRotation, cameraElevation, currentDisplayMode <> DisplayMode.Dialogue)
                    test += 1
                Else
                    '// Clear reflection buffer if reflections are disabled
                    glBindFramebuffer(GL_FRAMEBUFFER, worldRenderer.reflectionFramebuffer)
                    '// Set default colour to light blue
                    glClearColor(0.4, 0.6, 1.0, 1)
                    glClear(GL_COLOUR_BUFFER_BIT)
                    glBindFramebuffer(GL_FRAMEBUFFER, 0)
                End If
                '// Render world to screen
                worldRenderer.RenderWorld(character, monitorSize, enemies, NPCs, lightSource, shadowFocusPoint, chunkBuffer, terrainVAO, grassProgram, grassVAO, textureGrass, totT, timeOfDay, False, ripples, 1 - deathProgress, Not mainMenu, isRaining, rainProgram, cameraPosition, cameraRotation, cameraElevation, currentDisplayMode <> DisplayMode.Dialogue)
            End If
#End Region

            '// Used for debugging, press R to make it rain
#If RAIN_DEBUG Then
            If KeyboardInput.KeyDown("R"c) Then
                timeToWeatherChange = -1
            End If
#End If
            '// Randomly change the weather
            WeatherControl(isRaining, soundRainInstance, audio, soundRain, timeToWeatherChange, timeOfDay)

            '// Update entities and select the closest
            If currentDisplayMode = DisplayMode.MainGame Then
                character.targetLock = KeyboardInput.KeyDown("Q"c)
                If character.targetLock Then

                Else
                    enemyFocusIndex = enemies.GetClosest()
                    If enemyFocusIndex > -1 Then
                        character.targetFocusIndex = enemyFocusIndex
                    End If
                End If
                enemies.Update(deltaT)
                enemies.RefreshSpawn()
                NPCs.Update(deltaT)
            End If

            '// Display game over message when death animation has finished
            If deathProgress >= 1 Then
                activeForm = Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmGameOver)
            End If

            '// Respawn character
            If respawnFlag Then
                Respawn(character, enemies, NPCs, activeForm)
            End If

            '// Get keyboard inputs
            KeyboardInput.NextFrame()
            If activeForm = -1 And KeyboardInput.KeyPressed(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.Pause)) Then
                KeyboardInput.KeyPressed(keyBindings.GetKeyBinds(KeyboardInput.KeyBinds.Pause))
                activeForm = Menu.GetFormIndexByID(forms, Menu.Controls.ID_frmPaused)
            End If

            '// Render either the health or the current form
            If activeForm = -1 Then
                MatrixGenerator.GeneratePlayerMatrices(matrixRelative, matrixView, matrixPerspective, monitorSize, character, False, cameraPosition, cameraRotation, cameraElevation)
                healthController.RenderHealth(character, graphicsBindings, matrixPerspective, enemies)
                context.glBindVertexArray(1)
                context.glBindTexture(GL_TEXTURE_0, formInterface.fontTextureMap.textureID)
                formInterface.LoadForm(gameplayForm, formInterface.relMouse, False, False)
                formInterface.RenderScreen(False)
            Else
                DisplayForms(formInterface, forms, activeForm, keysPressed, dMouse, context)
            End If

            glFinish()

            '// Press L to display the player's coordinates, used for debugging
#If DEBUG Then
            If KeyboardInput.KeyDown("L"c) Then
                SetCursorPosition(0, 0)
                WriteLine(character.location.x & " " & character.location.y & " " & character.location.z)
                ReadLine()
            End If
#End If
        Loop
    End Sub

    '// Randomly changes the weather
    Private Shared Sub WeatherControl(ByRef isRaining As Boolean, ByRef soundRainInstance As Integer, ByRef audio As AudioManager, ByVal soundRain As Integer, ByRef timeToWeatherChange As Single, timeOfDay As Single)
        If timeToWeatherChange < 0 Then
            '// Pick new random time to change the weather
            timeToWeatherChange = (Rnd() + 1) * RAIN_DURATION
            isRaining = Not isRaining
            If isRaining Then
                '// Play rain noises
                soundRainInstance = audio.PlaySound(soundRain, True, False, New COORD3Sng(0, 0, 0), 1)
            Else
                '// Do not play rain noises when it is sunny
                audio.StopSound(soundRainInstance)
            End If
        End If
        If (timeOfDay + Math.PI / 2) Mod (2 * Math.PI) > Math.PI Then
            isRaining = False
            '// No rain at night
        End If
    End Sub

    '// Subroutine to handle dialogue with NPCs
    Private Shared Sub Talk(ByRef cameraPosition As COORD3Sng, ByRef cameraRotation As Single, ByRef cameraElevation As Single, ByRef NPCs As NPCManager, ByVal deltaT As Single, ByRef activeForm As Integer)
        Dim talkTarget As NPC = NPCs.NPCList(character.talkTarget)

        '// Force NPC to have idle animation
        talkTarget.actionInProgress = False
        talkTarget.animationName = "idle"
        talkTarget.animationProgress = CSng(Timer)

        '// Next dialogue if current dialogue is an action or the player presses enter
        If KeyboardInput.KeyPressed(13) Or NPCs.GetCommand()(0) = "!"c Then
            If NPCs.AdvanceDialogue() Then
                '// Exit if end of dialogue reached
                activeForm = -1
                character.talkTarget = -1
            End If
        End If

        '// Display current dialogue
        If activeForm <> -1 Then
            NPCs.RefreshDialogue(deltaT)
        End If
    End Sub

    '// Get ID of current form
    Private Shared Function GetFormID(forms As GUIObject.Form(), activeForm As Integer) As Integer
        If activeForm = -1 Then Return -1
        Return forms(activeForm).ID
    End Function

    '// Camera may be 1st or 3rd person, or could point at an NPC
    Private Shared Sub ControlCamera(ByRef cameraPosition As COORD3Sng, ByRef cameraRotation As Single, ByRef cameraElevation As Single, formID As Integer, NPCs As NPCManager, mainMenu As Boolean, prevMainMenu As Boolean)
        Dim talkTarget As NPC
        If currentDisplayMode = DisplayMode.MainGame Then
            '// Controlled by player and current 1st / 3rd person setup
            cameraPosition.x = character.cameraPosition.x
            cameraPosition.y = character.cameraPosition.y
            cameraPosition.z = character.cameraPosition.z
            cameraRotation = character.cameraRotation
            cameraElevation = character.cameraElevation
        ElseIf formID = Menu.Controls.ID_frmLoadGame Then
            '// Camera is above the ground when in the menu
            cameraPosition.x = character.cameraPosition.x
            cameraPosition.y = character.cameraPosition.y + 3
            cameraPosition.z = character.cameraPosition.z
        ElseIf currentDisplayMode = DisplayMode.Dialogue Then
            '// Point camera at NPC
            talkTarget = NPCs.NPCList(character.talkTarget)
            cameraPosition.x = CSng(talkTarget.location.x + 1.5 * Math.Sin(talkTarget.rotation))
            cameraPosition.z = CSng(talkTarget.location.z + 1.5 * Math.Cos(talkTarget.rotation))
            cameraPosition.y = talkTarget.location.y + 1.8F
            cameraRotation = talkTarget.rotation + CSng(Math.PI)
            cameraElevation = 0
        ElseIf mainMenu And Not prevMainMenu Then
            '// Pick random location for the camera when showcasing the terrain
            Randomize()
            Do
                cameraPosition = New COORD3Sng(Rnd() * 1000, 0, Rnd() * 1000)
            Loop Until PerlinNoise.GetHeight(cameraPosition.x, cameraPosition.z, randoms) > 0.45
            character.location = cameraPosition
        End If
    End Sub

    '// Check if saved game folder is non-empty and exists
    Private Shared Function LoadGameExists() As Boolean
        If Not IO.Directory.Exists("Saved_Games") Then
            Return False
        End If
        Return IO.Directory.GetDirectories("Saved_Games").Length > 0
    End Function

    '// Set an enum based on the current form being displayed
    Private Shared Function GetDisplayMode(currentForm As Integer, forms As GUIObject.Form()) As DisplayMode
        If currentForm = -1 Then Return DisplayMode.MainGame
        Select Case forms(currentForm).ID
            Case Menu.Controls.ID_frmCharacter, Menu.Controls.ID_frmCrafting, Menu.Controls.ID_frmInventory, Menu.Controls.ID_frmMissions
                Return DisplayMode.Inventory
            Case Menu.Controls.ID_frmGameOver, Menu.Controls.ID_frmPaused
                Return DisplayMode.PausedState
            Case Menu.Controls.ID_frmLoadGame, Menu.Controls.ID_frmMain, Menu.Controls.ID_frmNewGame, Menu.Controls.ID_frmSettings, Menu.Controls.ID_frmSettingsGraphics, Menu.Controls.ID_frmSettingsInput
                Return DisplayMode.MainMenu
            Case Menu.Controls.ID_frmTalk
                Return DisplayMode.Dialogue
        End Select
    End Function

    '// Generate new heightmaps if the player has moved too far
    '// Heightmaps generated dynamically to reduce memory usage
    Private Shared Sub RegenerateTerrain(offset As COORD2Sng, terrainProgram As UInt32, grassProgram As UInt32, shadowProgram As UInt32, terrainGenerator As TerrainGenerator, monitorSize As COORD2Short, ByRef context As OpenGLContext)
        terrainGenerator.GenerateTerrain(monitorSize, offset)
        context.glUseProgram(terrainProgram)
        glUniform2f(glGetUniformLocationStr(CInt(terrainProgram), "terrainOffset"), offset.x, offset.y)
        context.glUseProgram(grassProgram)
        glUniform2f(glGetUniformLocationStr(CInt(grassProgram), "terrainOffset"), offset.x, offset.y)
        context.glUseProgram(shadowProgram)
        glUniform2f(glGetUniformLocationStr(CInt(shadowProgram), "terrainOffset"), offset.x, offset.y)
        TerrainRenderer.BindTiledTextures(CInt(terrainProgram), CUInt(Marshal.ReadInt32(Sky.texPtr)))
    End Sub

    '// Function called by the respawn form to communicate with the main game
    Public Shared Sub Respawn(ByRef formArr As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        respawnFlag = True
    End Sub

    '// Respawn the character
    Private Shared Sub Respawn(ByRef character As Player, ByRef enemies As EnemyManager, ByRef NPCs As NPCManager, ByRef activeForm As Integer)
        '// Prevent continuous respawn
        respawnFlag = False
        '// Reset health, positions and entities
        character.health = 100
        character.location = New COORD3Sng(300, 0, 300)
        deathProgress = 0
        activeForm = -1
        enemies.enemies.Clear()
        NPCs.Respawn()
        character.missionList.Respawn()
    End Sub

    '// Display forms if not in the main game
    Private Shared Sub DisplayForms(ByRef formInterface As GUIInstance, ByRef forms As GUIObject.Form(), ByRef activeForm As Integer, ByRef keysPressed As Boolean(), deltaMouse As MouseInput.POINT, ByRef context As OpenGLContext)
        Menu.DisplayMenu(formInterface, forms(activeForm), False, keysPressed, deltaMouse, context)
    End Sub

    '// Apply user input to the character
    Private Shared Sub ControlCharacter(ByRef character As Player, deltaT As Single, dMouse As MouseInput.POINT, randoms As Single(), mobs As Mob(), NPCs As NPCManager, deathAnimation As Boolean)
        Dim oldPosition As New COORD3Sng
        oldPosition.x = character.location.x
        oldPosition.y = character.location.y
        oldPosition.z = character.location.z
        If Not deathAnimation Then
            '// Move and regenerate health
            character.MouseMovement(deltaT, dMouse)
            character.MouseClicks(deltaT, mobs, NPCs)
            character.KeyboardMovement(deltaT)
            character.RegenerateHealth(deltaT)
            character.SmoothRotation(deltaT)
        End If
        '// Used to test an animation
#If DEBUG Then
        If character.animationName = "blockbreak" Then
            IO.File.AppendAllText("Debug.txt", character.animationList.Count & " " & character.animationProgress & vbNewLine)
        End If
#End If
        '// Advance animation frame
        character.animationProgress += deltaT * character.model.animations.GetAnimationFrame(character.animationName).speed
        '// Freeze animation if death animation has finished
        If deathAnimation Then
            character.animationProgress = Math.Min(character.animationProgress, CSng(character.model.animations.GetAnimationFrame(character.animationName).data.Length - 1) / 20)
        End If
        '// Walking and running animation translations are ignored and controlled by the program
        If character.bypassAnimationTranslation Then
            character.UpdateAnimationPosition(False)
        Else
            character.UpdateAnimationPosition(True)
        End If
        character.deltaPosition.x = (character.location.x - oldPosition.x) / deltaT
        character.deltaPosition.y = (character.location.y - oldPosition.y) / deltaT
        character.deltaPosition.z = (character.location.z - oldPosition.z) / deltaT
    End Sub

    '// Classes, structures and enums

    Public Class GraphicsSettings
        Private graphicsSettings(3) As Integer
        Public Function GetGraphicsSettings(index As Integer) As Integer
            Return graphicsSettings(index)
        End Function
        Public Sub SetGraphicsSettings(index As Integer, value As Integer)
            graphicsSettings(index) = value
            If Not IO.Directory.Exists("Settings") Then IO.Directory.CreateDirectory("Settings")
            SaveGame.WriteAllInt("Settings\Graphics.settings", graphicsSettings)
        End Sub
        Sub New()
            If IO.Directory.Exists("Settings") AndAlso IO.File.Exists("Settings\Graphics.settings") Then
                graphicsSettings = SaveGame.ReadAllInt("Settings\Graphics.settings")
            Else
                graphicsSettings = {1, 1, 1, 0}
            End If
        End Sub
        '// Set default settings (all enabled apart from numeric health)
        Public Sub ResetGraphics(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
            For i = 0 To graphicsSettings.Length - 1
                SetGraphicsSettings(i, {1, 1, 1, 0}(i))
            Next
        End Sub

        Public Enum SettingsIndex
            Reflection = 0
            Shadow = 1
            HealthBar = 2
            NumericHealth = 3
        End Enum
        Public Shared graphicsBindLabels As String() = IO.File.ReadAllLines("Resources\GraphicsBindLabels.txt")

    End Class

    Public Enum DisplayMode
        MainGame = 0
        MainMenu = 1
        Inventory = 2
        PausedState = 3
        Dialogue = 4
    End Enum

End Class

