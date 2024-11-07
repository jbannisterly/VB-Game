#Const DEBUG = False
Option Strict On
Imports System.Console
Imports NEA.OpenGLImporter
Imports NEA.GUIObject
'// Class to manage all the forms in the inventories and the menus
Public Class Menu
    '// Store current state of the menu and the instance used to render it
    Public Shared currentForm As Integer
    Public Shared formInterface As GUIInstance
    Public Shared menuStack As New AbstractDataType.Stack(Of Integer)(8)

    '// Debug subroutine to test form callback
    Private Shared Sub Test(frmParent As Form, arguments As Integer)
        Write(arguments)
        ReadLine()
    End Sub

    '// Subroutine that can be called by a button click, used to exit the program
    Private Shared Sub ExitProgram(ByRef frmArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        GracefulExit()
    End Sub

    '// Display a new game when the arrow buttons are clicked
    Private Shared Sub ChangeLoadGameItem(ByRef frmArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        Dim saveNameLabel As New Label
        Dim numWorlds As Integer = GameWorld.gamesToLoad.Length
        Dim currentWorldID As Integer = GameWorld.gameToLoadID

        '// Get reference to the title
        frmArray(currentForm).GetElementByID(Controls.ID_lblLoadGameName, saveNameLabel)
        '// Get index of the world selected
        currentWorldID = (arguments + currentWorldID + numWorlds) Mod numWorlds
        '// Update display details
        saveNameLabel.text = SaveGame.FileStringDisplay(GameWorld.gamesToLoad(currentWorldID).worldName)
        GameWorld.character.location = GameWorld.gamesToLoad(currentWorldID).globalData.playerPosition
        GameWorld.gameToLoadID = currentWorldID
    End Sub

    '// Respond to mouse input and change the position of the cursor on the screen
    Private Shared Sub UpdateMousePosition(ByRef relMouse As Form.COORD, ByVal deltaMouse As MouseInput.POINT)
        '// Convert 1 pixel to 0.005 of the screen
        relMouse.x += CSng(deltaMouse.x * 0.005)
        relMouse.y -= CSng(deltaMouse.y * 0.005)
        '// Clamp the mouse to the screen
        If relMouse.x < -1 Then relMouse.x = -1
        If relMouse.x > 1 Then relMouse.x = 1
        If relMouse.y < -1 Then relMouse.y = -1
        If relMouse.y > 1 Then relMouse.y = 1
    End Sub

    '// Generate vertex data from the current form and render to the screen
    Public Shared Sub DisplayMenu(ByRef formInterface As GUIInstance, ByRef frmCurrent As GUIObject.Form, ByVal clearScreen As Boolean, ByRef keysPressed As Boolean(), deltaMouse As MouseInput.POINT, ByRef context As OpenGLContext)
        Dim pressed As Integer()

        '// Update mouse label coordinates
        UpdateMousePosition(formInterface.relMouse, deltaMouse)

        '// Handle mouse hover and click events
        If MouseInput.MouseClick(MouseInput.Buttons.Left) Then
            frmCurrent.MouseClick(formInterface.relMouse)
        End If
        frmCurrent.MouseHover(formInterface.relMouse)
        pressed = KeyboardInput.GetKeysDown(keysPressed)

        '// Handle keyboard input for text fields
        If pressed.Length > 0 Then frmCurrent.KeyPressed(pressed)
#If DEBUG Then
        If frmCurrent.exitCondition Then
            'MsgBox(frmCurrent.arguments)
        End If
#End If

        frmCurrent.UpdateForm()

        '// Bind textures and vertex array so it does not overwrite existing buffers
        context.glBindVertexArray(1)
        context.glBindTexture(GL_TEXTURE_0, formInterface.fontTextureMap.textureID)

        '// Generate vertex data
        formInterface.LoadForm(frmCurrent, formInterface.relMouse, True, False)
        '// Render all form
        formInterface.RenderScreen(clearScreen)
        '// Render images of 3D models
        formInterface.RenderModels(frmCurrent)
        '// Rebind vertex array and textures
        context.glBindVertexArray(1)
        context.glBindTexture(GL_TEXTURE_0, formInterface.fontTextureMap.textureID)
        '// Get vertex data and display controls that are rendered above the images
        formInterface.LoadForm(frmCurrent, formInterface.relMouse, True, True)
        formInterface.RenderScreen(False)
#If DEBUG Then
        If KeyboardInput.KeyDown(Asc("X")) Then
            GracefulExit()
        End If
#End If
    End Sub

    '// Default subroutine that can be called if no update action is required
    '// Used to prevent invoking a null pointer
    Public Shared Sub FormUpdateDefault(frm As Form)
    End Sub


    '// Subroutine to enable or disable the play game button based on whether details have been entered
    Public Shared Sub GameLoadSubroutine(frm As Form)
        Dim playGame As New Label
        Dim txtSaveName As New Label
        Dim txtPassword As New Label
        If frm.GetElementByID(Controls.ID_btnPlayGame, playGame) And frm.GetElementByID(Controls.ID_txtSaveName, txtSaveName) And frm.GetElementByID(Controls.ID_txtPassword, txtPassword) Then
            playGame.enabled = txtSaveName.text <> "" And txtPassword.text <> ""
        End If
    End Sub

    '// Return an array of forms used in the inventory pages
    Private Shared Function InitialiseInventoryForms(ByRef context As OpenGLContext) As Form()
        '// Declaration of forms and controls
        Dim formList As New List(Of Form)
        Dim lblBackground As New Label
        Dim btnInventory As New Button
        Dim btnCharacter As New Button
        Dim btnCrafting As New Button
        Dim btnMissions As New Button
        Dim lblInventory As New Label
        Dim lblCharacter As New Label
        Dim lblCrafting As New Label
        Dim lblMissions As New Label
        Dim tabButtons As Button() = {btnInventory, btnCharacter, btnCrafting, btnMissions}
        Dim tabLabels As Label() = {lblInventory, lblCharacter, lblCrafting, lblMissions}
        Dim tabArguments As Integer() = {Controls.ID_frmInventory, Controls.ID_frmCharacter, Controls.ID_frmCrafting, Controls.ID_frmMissions}
        Dim tabText As String() = {"Inventory", "Character", "Crafting", "Missions"}
        Dim stlBtnTab As New StyleSheet
        Dim stlLblTab As New StyleSheet
        Dim stlBtnGrid As New StyleSheet
        Dim stlItemImg As New StyleSheet
        Dim stlItemCap As New StyleSheet
        Dim stlBackground2 As New StyleSheet

        '// Set styles for the background label
        lblBackground.SetCoords(-0.98, -0.98, 1.96, 1.8)
        lblBackground.SetBackgroundColour(New Colour("6C5200"))
        lblBackground.borderWidth = 0.04
        lblBackground.SetBorderColour(New Colour("000000"))
        lblBackground.skipHover = True

        stlBackground2.SetBackgroundColour(New Colour("FFAA00"))
        stlBackground2.SetBorderColour(New Colour("440000"))
        stlBackground2.borderWidth = 0.01

        '// Initialise styles for controls
        stlBtnTab.SetBorderColour(New Colour("000000"))
        stlBtnTab.SetBackgroundColour(New Colour("6C5200"))
        stlBtnTab.borderWidth = 0.01
        stlBtnTab.fontColour = New Colour("000000")
        stlBtnTab.text = ""
        stlBtnTab.fontSize = 0.07

        stlLblTab.SetBorderColour(New Colour("000000"))
        stlLblTab.SetBackgroundGradient(New Colour("FFAA00"), New Colour("6C5200"))
        stlLblTab.borderWidth = 0
        stlLblTab.fontColour = New Colour("000000")
        stlLblTab.text = ""
        stlLblTab.fontSize = 0.09

        stlBtnGrid.fontSize = 0.1
        stlBtnGrid.SetBackgroundGradient(New Colour("B8A877"), New Colour("6C5200"))
        stlBtnGrid.borderWidth = 0.01
        stlBtnGrid.SetBorderColour(New Colour("000000"))

        stlItemImg.fontSize = 0.18
        stlItemImg.SetBackgroundColour(New Colour("FFFFFF"))
        stlItemImg.borderWidth = 0.01
        stlItemImg.SetBorderColour(New Colour("000000"))
        stlItemImg.text = ""

        stlItemCap.fontSize = 0.05
        stlItemCap.SetBackgroundColour(New Colour("FFFFFF"))
        stlItemCap.borderWidth = 0.01
        stlItemCap.SetBorderColour(New Colour("000000"))
        stlItemCap.fontColour = New Colour("000000")
        stlItemCap.text = ""

        '// Create tab controls for navigation between inventory forms
        For i = 0 To tabButtons.Length - 1
            tabButtons(i).SetStyles(stlBtnTab)
            tabButtons(i).EventClick = AddressOf ChangeForm
            tabButtons(i).eventArgs = tabArguments(i)
            tabButtons(i).text = tabText(i)

            tabLabels(i).SetStyles(stlLblTab)
            tabLabels(i).text = tabText(i)
        Next

        '// Tabs can be either buttons or labels
        '// The tab for the current form is a label and is larger to highlight it
        '// This cannot be clicked on, as it would point to the current form
        btnInventory.SetCoords(-0.8, 0.84, 0.3, 0.1)
        btnCharacter.SetCoords(-0.4, 0.84, 0.3, 0.1)
        btnCrafting.SetCoords(-0.0, 0.84, 0.3, 0.1)
        btnMissions.SetCoords(0.4, 0.84, 0.3, 0.1)

        lblInventory.SetCoords(-0.83, 0.84, 0.36, 0.12)
        lblCharacter.SetCoords(-0.43, 0.84, 0.36, 0.12)
        lblCrafting.SetCoords(-0.03, 0.84, 0.36, 0.12)
        lblMissions.SetCoords(0.37, 0.84, 0.36, 0.12)

        '// Initialise all forms using the base inventory form
        formList.Add(InitialiseInventoryForm(InitialiseInventoryFormShared(0, lblBackground, tabButtons, tabLabels), context, stlBtnGrid, stlItemImg, stlItemCap, stlBackground2))
        formList.Add(InitialiseCharacterForm(InitialiseInventoryFormShared(1, lblBackground, tabButtons, tabLabels), context))
        formList.Add(InitialiseCraftingForm(InitialiseInventoryFormShared(2, lblBackground, tabButtons, tabLabels), context, stlBtnGrid, stlItemCap, stlItemImg, stlBackground2))
        formList.Add(InitialiseMissionForm(InitialiseInventoryFormShared(3, lblBackground, tabButtons, tabLabels)))

        Return formList.ToArray()
    End Function

    '// Create a new form for the inventory
    Private Shared Function InitialiseInventoryForm(baseForm As Form, ByRef context As OpenGLContext, stlBtnGrid As StyleSheet, stlItemImg As StyleSheet, stlItemCap As StyleSheet, stlBackground2 As StyleSheet) As Form
        '// Declaration of controls
        Dim frmInventory As Form = baseForm
        Dim stlCombo As New StyleSheet
        Dim btnEquip As New Button
        Dim renLargeImage As New RenderTarget(context)
        Dim lblItemDescription As New Label
        Dim lblItemBackground As New Label
        Dim inventoryItems As GridView
        Dim cmbSort As New DropDown
        Dim cmbFilter As New DropDown
        Dim btnSortDropDown As New Button
        Dim btnFilterDropDown As New Button
        Dim lblSort As New Label
        Dim lblFilter As New Label

        '// Initialise style sheet
        stlCombo.SetBackgroundColour(New Colour("FFFFFF"))
        stlCombo.SetCoords(0, 0.73, 0.3, 0.07)
        stlCombo.fontSize = 0.07
        stlCombo.fontColour = New Colour("000000")

        '// Initialise controls and set styles
        inventoryItems = New GridView(stlBtnGrid, stlItemCap, stlItemImg, context)
        inventoryItems.SetStyles(stlBackground2)
        inventoryItems.SetAllCoords(-0.9, -0.9, 1.3, 1.6)

        lblItemBackground.SetStyles(stlBackground2)
        lblItemBackground.SetCoords(0.45, -0.9, 0.5, 1.6)

        renLargeImage.SetStyles(stlItemCap)
        renLargeImage.SetCoords(0.5, 0, 0.4, 0.6)
        renLargeImage.text = ""
        renLargeImage.position = New CoordDataTypes.COORD3Sng(0, 0, -0.5)
        renLargeImage.modelElevation = CSng(Math.PI / 4)
        renLargeImage.rotate = True

        lblItemDescription.SetStyles(stlItemCap)
        lblItemDescription.SetCoords(0.5, -0.7, 0.4, 0.6)
        lblItemDescription.text = ""

        btnEquip.SetStyles(stlBtnGrid)
        btnEquip.SetCoords(0.5, -0.8, 0.4, 0.1)
        btnEquip.text = "Equip item"
        btnEquip.fontSize = 0.1

        btnFilterDropDown.EventClick = AddressOf ButtonTest
        btnSortDropDown.EventClick = AddressOf ButtonTest

        cmbSort.SetStyles(stlCombo)
        cmbSort.x = -0.7
        cmbSort.ListItemBase = btnSortDropDown
        cmbSort.CopyStylesToList()
        cmbSort.ListItems.Add("Name")
        cmbSort.ListItems.Add("Power")
        cmbSort.ListItems.Add("Type")
        cmbSort.ID = Controls.ID_cmbSort
        cmbSort.topZ = True
        btnSortDropDown.topZ = True
        btnSortDropDown.SetBackgroundColour(New Colour("DDDDDD"))

        cmbFilter.SetStyles(stlCombo)
        cmbFilter.x = -0.2
        cmbFilter.ListItemBase = btnFilterDropDown
        cmbFilter.CopyStylesToList()
        cmbFilter.ListItems.Add("No Filter")
        cmbFilter.ListItems.Add("Resource")
        cmbFilter.ListItems.Add("Weapon")
        cmbFilter.ListItems.Add("Shield")
        cmbFilter.ID = Controls.ID_cmbFilter
        cmbFilter.topZ = True
        btnFilterDropDown.topZ = True
        btnFilterDropDown.SetBackgroundColour(New Colour("DDDDDD"))

        lblSort.SetStyles(stlCombo)
        lblSort.x = -0.85
        lblSort.text = "Sort"
        lblSort.transparentBackground = True
        lblFilter.SetStyles(stlCombo)
        lblFilter.x = -0.35
        lblFilter.text = "Filter"
        lblFilter.transparentBackground = True

        '// Add controls to the form
        frmInventory.NewChild(inventoryItems)
        frmInventory.NewChild(lblItemBackground)
        frmInventory.NewChild(renLargeImage)
        frmInventory.NewChild(lblItemDescription)
        frmInventory.NewChild(btnEquip)
        frmInventory.NewChild(cmbSort)
        frmInventory.NewChild(cmbFilter)
        frmInventory.NewChild(lblSort)
        frmInventory.NewChild(lblFilter)

        '// Set IDs of controls to be refereced
        frmInventory.ID = Controls.ID_frmInventory
        inventoryItems.ID = Controls.ID_grdInventory
        renLargeImage.ID = Controls.ID_renItemDetailImage
        lblItemDescription.ID = Controls.ID_lblItemDetail
        btnEquip.ID = Controls.ID_btnEquipItem

        Return frmInventory
    End Function

    '// Create the character information form
    Private Shared Function InitialiseCharacterForm(baseForm As Form, ByRef context As OpenGLContext) As Form
        '// Declaration of controls
        Dim frmCharacter As Form = baseForm
        Dim renCharacter As New RenderTarget(context)
        Dim lblStats As New Label
        Dim lblItems As New Label
        Dim lblStatsTitle As New Label
        Dim lblItemsTitle As New Label
        Dim stlContent As New StyleSheet
        Dim stlHeader As New StyleSheet

        '// Initialise styles
        stlContent.SetBackgroundColour(New Colour("FFFFFF"))
        stlContent.SetBorderColour(New Colour("000000"))
        stlContent.fontSize = 0.1
        stlHeader.SetBackgroundColour(New Colour("FFFFFF"))
        stlHeader.SetBorderColour(New Colour("000000"))
        stlHeader.fontSize = 0.15

        '// Add controls to the form
        frmCharacter.NewChild(renCharacter)
        frmCharacter.NewChild(lblStats)
        frmCharacter.NewChild(lblItems)
        frmCharacter.NewChild(lblStatsTitle)
        frmCharacter.NewChild(lblItemsTitle)

        '// Intialise controls and set styles
        renCharacter.SetStyles(stlContent)
        renCharacter.SetCoords(-0.95, -0.9, 0.6, 1.5)
        renCharacter.position = New CoordDataTypes.COORD3Sng(0, 1, -1.2)
        renCharacter.rotate = False

        lblStats.SetStyles(stlContent)
        lblStats.SetCoords(-0.3, -0.9, 0.6, 1.3)
        lblStats.ID = Controls.ID_lblCharacterStats

        lblItems.SetStyles(stlContent)
        lblItems.SetCoords(0.35, -0.9, 0.6, 1.3)
        lblItems.ID = Controls.ID_lblCharacterItems

        lblItemsTitle.SetStyles(stlHeader)
        lblItemsTitle.text = "Items"
        lblItemsTitle.SetCoords(0.35, 0.4, 0.6, 0.2)
        lblStatsTitle.SetStyles(stlHeader)
        lblStatsTitle.text = "Stats"
        lblStatsTitle.SetCoords(-0.3, 0.4, 0.6, 0.2)

        '// Set IDs of the form
        frmCharacter.ID = Controls.ID_frmCharacter
        renCharacter.ID = Controls.ID_renCharacter

        Return frmCharacter
    End Function

    '// Create the crafting form
    Private Shared Function InitialiseCraftingForm(baseForm As Form, ByRef context As OpenGLContext, stlBtnGrid As StyleSheet, stlItemCap As StyleSheet, stlItemImg As StyleSheet, stlBackground2 As StyleSheet) As Form
        '// Declaration of controls
        Dim frmCrafting As Form = baseForm
        Dim recipes As GridView
        Dim lblItemBackground As New Label
        Dim renLargeImage As New RenderTarget(context)
        Dim lblRecipeDescription As New Label
        Dim btnCraft As New Button

        '// Initialise controls and set styles
        recipes = New GridView(stlBtnGrid, stlItemCap, stlItemImg, context)
        recipes.SetStyles(stlBackground2)
        recipes.SetAllCoords(-0.9, -0.9, 1.3, 1.6)

        lblItemBackground.SetStyles(stlBackground2)
        lblItemBackground.SetCoords(0.45, -0.9, 0.5, 1.6)

        renLargeImage.SetStyles(stlItemCap)
        renLargeImage.SetCoords(0.5, 0, 0.4, 0.6)
        renLargeImage.text = ""
        renLargeImage.position = New CoordDataTypes.COORD3Sng(0, 0, -0.5)
        renLargeImage.modelElevation = CSng(Math.PI / 4)
        renLargeImage.rotate = True

        lblRecipeDescription.SetStyles(stlItemCap)
        lblRecipeDescription.SetCoords(0.5, -0.7, 0.4, 0.6)
        lblRecipeDescription.text = ""

        btnCraft.SetStyles(stlBtnGrid)
        btnCraft.SetCoords(0.5, -0.8, 0.4, 0.1)
        btnCraft.text = "Craft item"
        btnCraft.fontSize = 0.1

        '// Add controls to form
        frmCrafting.NewChild(recipes)
        frmCrafting.NewChild(lblItemBackground)
        frmCrafting.NewChild(renLargeImage)
        frmCrafting.NewChild(lblRecipeDescription)
        frmCrafting.NewChild(btnCraft)

        '// Set IDs of the controls so they can be extracted and referenced
        renLargeImage.ID = Controls.ID_renRecipeDetailImage
        lblRecipeDescription.ID = Controls.ID_lblRecipeDetail
        btnCraft.ID = Controls.ID_btnCraftRecipe
        recipes.ID = Controls.ID_grdCrafting
        frmCrafting.ID = Controls.ID_frmCrafting

        Return frmCrafting
    End Function

    '// Create an inventory mission form
    Private Shared Function InitialiseMissionForm(baseForm As Form) As Form
        '// Control declaration
        Dim frmMissions As Form = baseForm
        Dim listMissions As New TaskList
        Dim listTasks As New TaskList
        Dim missionBase As New TaskItem(New Colour("00FF00"))
        Dim taskBase As New TaskItem(New Colour("00FF00"))

        '// Initialise controls and set styles
        missionBase.fontSize = 0.06
        taskBase.fontSize = 0.06
        taskBase.SetBackgroundColour(New Colour("FFFEDE"))
        taskBase.fontColour = New Colour("000000")
        missionBase.SetBackgroundColour(New Colour("FFFEDE"))
        missionBase.fontColour = New Colour("000000")

        listMissions.taskItemBase = missionBase
        listTasks.taskItemBase = taskBase

        listMissions.SetCoords(-0.95, -0.9, 0.925, 1.6)
        listMissions.SetBackgroundColour(New Colour("FFFEDE"))
        listTasks.SetCoords(0.025, -0.9, 0.925, 1.6)
        listTasks.SetBackgroundColour(New Colour("FFFEDE"))

        '// Add controls to forms
        frmMissions.NewChild(listMissions)
        frmMissions.NewChild(listTasks)

        '// Set IDs of controls
        frmMissions.ID = Controls.ID_frmMissions
        listMissions.ID = Controls.ID_lstMissions
        listTasks.ID = Controls.ID_lstTasks

        Return frmMissions
    End Function

    '// Initialise the base form for the inventory
    Private Shared Function InitialiseInventoryFormShared(formIndex As Integer, lblInventoryBackground As Label, inventoryTabs As Button(), selectedInventoryTabs As Label()) As Form
        Dim frmShared As New Form()
        '// Add navagation buttons to the top of the form
        For i = 0 To 3
            '// Do not add a button that links to the current form
            If formIndex <> i Then
                frmShared.NewChild(inventoryTabs(i))
            End If
        Next
        '// Add a background and a label displaying the currently selected form
        frmShared.NewChild(lblInventoryBackground)
        frmShared.NewChild(selectedInventoryTabs(formIndex))

        Return frmShared
    End Function

    '// Create an array of forms representing those used in the settings menu
    Private Shared Function InitialiseSettingsForms(btnBack As Button, btnClose As Button, keyBinding As KeyboardInput.KeyBinding, stlButton As StyleSheet, ByRef graphicsBinding As GameWorld.GraphicsSettings) As Form()
        '// Declaration of forms an controls
        Dim frmSettings As New Form
        Dim frmSettingsInput As New Form
        Dim frmSettingsGraphics As New Form
        Dim stlLblTitle As New StyleSheet
        Dim lblTitleMain As New Label
        Dim lblTitleInput As New Label
        Dim lblTitleGraphics As New Label
        Dim btnInput As New Button
        Dim btnGraphics As New Button
        Dim btnResetInputs As New Button
        Dim btnResetGraphics As New Button
        Dim keyBinds(KeyboardInput.keyBindLabels.Length - 1) As KeyBind
        Dim graphicsBinds(GameWorld.GraphicsSettings.graphicsBindLabels.Length - 1) As ToggleButtonLabel

        '// Create a new toggle button for each graphics setting
        For i = 0 To graphicsBinds.Length - 1
            graphicsBinds(i) = New ToggleButtonLabel(graphicsBinding)
            graphicsBinds(i).SetStyles(stlButton)
            graphicsBinds(i).SetCoords(-0.5, 0.4F - 0.15F * i, 0.8, 0.1)
            graphicsBinds(i).fontSize = 0.1
            graphicsBinds(i).borderWidth = 0.01
            graphicsBinds(i).text = GameWorld.GraphicsSettings.graphicsBindLabels(i)
            graphicsBinds(i).toggle.graphicsSettingsIndex = i
        Next

        '// Create a new key bind control for each input
        For i = 0 To keyBinds.Length - 1
            keyBinds(i) = New KeyBind(keyBinding, CType(i, KeyboardInput.KeyBinds))
            keyBinds(i).SetStyles(stlButton)
            keyBinds(i).SetCoords(-0.6F + 0.7F * (i \ 4), 0.4F - 0.15F * (i Mod 4), 0.5, 0.1)
            keyBinds(i).fontSize = 0.07
            keyBinds(i).borderWidth = 0.01
            keyBinds(i).text = KeyboardInput.keyBindLabels(i)
        Next

        '// Set styles, IDs and subroutines to call of controls
        stlLblTitle.transparentBackground = True
        stlLblTitle.fontSize = 0.2
        stlLblTitle.SetCoords(-0.5, 0.7, 1, 0.2)
        stlLblTitle.fontColour = New Colour("000000")

        frmSettings.ID = Controls.ID_frmSettings
        frmSettingsInput.ID = Controls.ID_frmSettingsInput
        frmSettingsGraphics.ID = Controls.ID_frmSettingsGraphics
        frmSettings.UpdateSubroutine = AddressOf FormUpdateDefault
        frmSettingsInput.UpdateSubroutine = AddressOf FormUpdateDefault
        frmSettingsGraphics.UpdateSubroutine = AddressOf FormUpdateDefault

        lblTitleMain.SetStyles(stlLblTitle)
        lblTitleInput.SetStyles(stlLblTitle)
        lblTitleGraphics.SetStyles(stlLblTitle)

        lblTitleMain.text = "Settings"
        lblTitleInput.text = "Input Settings"
        lblTitleGraphics.text = "Graphics Settings"

        btnInput.SetStyles(stlButton)
        btnInput.fontSize = 0.15
        btnInput.text = "Input Settings"
        btnInput.EventClick = AddressOf ChangeForm
        btnInput.eventArgs = Controls.ID_frmSettingsInput

        btnGraphics.SetStyles(stlButton)
        btnGraphics.fontSize = 0.15
        btnGraphics.y -= 0.4F
        btnGraphics.text = "Graphics Settings"
        btnGraphics.EventClick = AddressOf ChangeForm
        btnGraphics.eventArgs = Controls.ID_frmSettingsGraphics

        btnResetInputs.SetStyles(stlButton)
        btnResetInputs.y -= 0.6F
        btnResetInputs.text = "Reset Inputs"
        btnResetInputs.EventClick = AddressOf keyBinding.ResetInputs
        btnResetInputs.fontSize = 0.15

        btnResetGraphics.SetStyles(stlButton)
        btnResetGraphics.y -= 0.6F
        btnResetGraphics.text = "Reset Graphics"
        btnResetGraphics.EventClick = AddressOf graphicsBinding.ResetGraphics
        btnResetGraphics.fontSize = 0.15

        '// Add controls to forms
        frmSettings.NewChild(btnBack)
        frmSettingsInput.NewChild(btnBack)
        frmSettingsGraphics.NewChild(btnBack)
        frmSettings.NewChild(btnClose)
        frmSettingsInput.NewChild(btnClose)
        frmSettingsGraphics.NewChild(btnClose)

        frmSettings.NewChild(lblTitleMain)
        frmSettings.NewChild(btnInput)
        frmSettings.NewChild(btnGraphics)

        frmSettingsInput.NewChild(lblTitleInput)
        For i = 0 To keyBinds.Length - 1
            frmSettingsInput.NewChild(keyBinds(i))
        Next
        frmSettingsInput.NewChild(btnResetInputs)

        frmSettingsGraphics.NewChild(lblTitleGraphics)
        For i = 0 To graphicsBinds.Length - 1
            frmSettingsGraphics.NewChild(graphicsBinds(i))
        Next
        frmSettingsGraphics.NewChild(btnResetGraphics)

        Return {frmSettings, frmSettingsInput, frmSettingsGraphics}
    End Function

    '// Returns a stylesheet so labels in different forms can have the same styles
    Public Shared Function GetLabelStyle() As StyleSheet
        Dim stlLabel As New StyleSheet
        stlLabel.fontSize = 0.2
        stlLabel.SetCoords(-0.7, 0.5, 1.6, 0.2)
        stlLabel.borderWidth = 0
        stlLabel.transparentBackground = True
        Return stlLabel
    End Function

    '// Generate an array of all forms used in the game with the exception of the one that displays health bars
    Public Shared Function InitialiseForms(ByRef formInterface As GUIInstance, ByRef keyBindings As KeyboardInput.KeyBinding, ByRef graphicsBindings As GameWorld.GraphicsSettings, context As OpenGLContext) As Form()
        formInterface = New GUIInstance(Window.GetSize(), context)
        Dim keysPressed(127) As Boolean
        Dim formList As New List(Of Form)

        '// Declaration of fomrs and controls
        Dim btnNewGame As New Button
        Dim btnLoadGame As New Button
        Dim btnClose As New Button
        Dim btnBack As New Button
        Dim btnPlayGame As New Button
        Dim btnRespawn As New Button
        Dim btnResume As New Button
        Dim frmGameplay As New Form
        Dim frmMain As New Form
        Dim frmNewGame As New Form
        Dim frmLoadGame As New Form
        Dim frmGameOver As New Form
        Dim frmPaused As New Form
        Dim frmTalk As New Form
        Dim txtSaveName As New TextBox
        Dim txtPassword As New TextBoxPassword
        Dim stlButton As New StyleSheet
        Dim stlText As New StyleSheet
        Dim stlLabel As StyleSheet = GetLabelStyle()
        Dim stlBtnTab As New StyleSheet
        Dim lblGameOver As New Label
        Dim lblPaused As New Label
        Dim lblTitle As New Label
        Dim lblLoadGameName As New Label
        Dim btnLoadGameNext As New Button
        Dim btnLoadGamePrev As New Button
        Dim btnSettings As New Button
        Dim btnMainMenuReturn As New Button
        Dim lblInvalidReason As New Label
        Dim lblTalk As New Label

        Dim lblHealth As New Label

        '// Initialise styles
        stlButton.SetBorderColour(New Colour("000000"))
        stlButton.SetBackgroundGradient(New Colour("B8A877"), New Colour("6C5200"))
        stlButton.fontSize = 0.2
        stlButton.SetCoords(-0.5, 0, 1, 0.2)
        stlButton.borderWidth = 0.1
        stlButton.fontColour = New Colour("000000")

        stlText.SetBorderColour(New Colour("000000"))
        stlText.SetBackgroundColour(New Colour("B8A877"))
        stlText.fontSize = 0.1
        stlText.SetCoords(-0.5, 0, 1, 0.1)
        stlText.borderWidth = 0.02
        stlText.text = ""
        stlText.fontColour = New Colour("000000")

        formInterface.relMouse.x = 0
        formInterface.relMouse.y = 0

        '// Add forms to a list of forms
        formList.Add(frmMain)
        formList.Add(frmNewGame)
        formList.Add(frmLoadGame)
        formList.Add(frmGameOver)
        formList.AddRange(InitialiseInventoryForms(context))
        formList.AddRange(InitialiseSettingsForms(btnBack, btnClose, keyBindings, stlButton, graphicsBindings))
        formList.Add(frmGameplay)
        formList.Add(frmPaused)
        formList.Add(frmTalk)

        '// Initialise update subroutines to be called every frame
        frmMain.UpdateSubroutine = AddressOf FormUpdateDefault
        frmLoadGame.UpdateSubroutine = AddressOf GameLoadSubroutine
        frmNewGame.UpdateSubroutine = AddressOf GameLoadSubroutine
        frmGameOver.UpdateSubroutine = AddressOf FormUpdateDefault
        frmGameplay.UpdateSubroutine = AddressOf FormUpdateDefault
        frmPaused.UpdateSubroutine = AddressOf FormUpdateDefault
        frmTalk.UpdateSubroutine = AddressOf FormUpdateDefault

        '// Set IDs of forms so they can be accessed and referenced outside this subroutine
        frmMain.ID = Controls.ID_frmMain
        frmNewGame.ID = Controls.ID_frmNewGame
        frmLoadGame.ID = Controls.ID_frmLoadGame
        frmGameOver.ID = Controls.ID_frmGameOver
        frmGameplay.ID = Controls.ID_frmGameplay
        frmPaused.ID = Controls.ID_frmPaused
        frmTalk.ID = Controls.ID_frmTalk

        '// Add controls to forms
        frmTalk.NewChild(lblTalk)

        frmMain.NewChild(btnNewGame)
        frmMain.NewChild(btnLoadGame)
        frmMain.NewChild(btnClose)
        frmMain.NewChild(lblTitle)
        frmMain.NewChild(btnSettings)

        frmNewGame.NewChild(btnClose)
        frmNewGame.NewChild(btnPlayGame)
        frmNewGame.NewChild(txtSaveName)
        frmNewGame.NewChild(txtPassword)
        frmNewGame.NewChild(btnBack)
        frmNewGame.NewChild(lblInvalidReason)

        frmLoadGame.NewChild(btnClose)
        frmLoadGame.NewChild(btnPlayGame)
        frmLoadGame.NewChild(txtPassword)
        frmLoadGame.NewChild(btnLoadGameNext)
        frmLoadGame.NewChild(btnLoadGamePrev)
        frmLoadGame.NewChild(lblLoadGameName)
        frmLoadGame.NewChild(btnBack)
        frmLoadGame.NewChild(lblInvalidReason)

        frmGameOver.NewChild(btnClose)
        frmGameOver.NewChild(lblGameOver)
        frmGameOver.NewChild(btnRespawn)
        frmGameOver.NewChild(btnMainMenuReturn)

        frmPaused.NewChild(btnClose)
        frmPaused.NewChild(lblPaused)
        frmPaused.NewChild(btnResume)
        frmPaused.NewChild(btnMainMenuReturn)

        frmGameplay.NewChild(lblHealth)

        '// Set styles of controls and add subroutine targets to button clicks
        lblTalk.fontSize = 0.07
        lblTalk.text = ""
        lblTalk.fontColour = New Colour("FFFFFF")
        lblTalk.transparentBackground = False
        lblTalk.SetBackgroundGradient(New Colour("3C2700"), New Colour("211A02"))
        lblTalk.SetBorderColour(New Colour("191002"))
        lblTalk.borderWidth = 0.02
        lblTalk.SetCoords(-0.8, -0.8, 1.6, 0.1)
        lblTalk.ID = Controls.ID_lblTalk

        lblHealth.SetStyles(stlLabel)
        lblHealth.text = "100"

        lblInvalidReason.SetStyles(stlLabel)
        lblInvalidReason.y = -0.8
        lblInvalidReason.fontSize = 0.1
        lblInvalidReason.text = ""
        lblInvalidReason.ID = Controls.ID_lblInvalidReason

        lblLoadGameName.SetStyles(stlLabel)
        lblLoadGameName.text = "Placeholder"
        lblLoadGameName.fontSize = 0.1
        lblLoadGameName.SetCoords(-0.5, 0.8, 1, 0.1)
        lblLoadGameName.ID = Controls.ID_lblLoadGameName

        lblGameOver.SetStyles(stlLabel)
        lblGameOver.text = "YOU DIED"
        lblGameOver.fontColour = New Colour("FF0000")

        lblPaused.SetStyles(stlLabel)
        lblPaused.text = "Paused"

        lblTitle.SetStyles(stlLabel)
        lblTitle.text = "VB Game"
        lblTitle.fontColour = New Colour("000000")

        btnLoadGameNext.SetStyles(stlButton)
        btnLoadGamePrev.SetStyles(stlButton)
        btnLoadGameNext.transparentBackground = True
        btnLoadGamePrev.transparentBackground = True
        btnLoadGamePrev.SetCoords(-0.7, 0.8, 0.15, 0.1)
        btnLoadGameNext.SetCoords(0.55, 0.8, 0.15, 0.1)
        btnLoadGameNext.fontSize = 0.1
        btnLoadGamePrev.fontSize = 0.1
        btnLoadGamePrev.text = "<Left>"
        btnLoadGameNext.text = "<Right>"
        btnLoadGamePrev.EventClick = AddressOf ChangeLoadGameItem
        btnLoadGameNext.EventClick = AddressOf ChangeLoadGameItem
        btnLoadGamePrev.eventArgs = -1
        btnLoadGameNext.eventArgs = 1

        btnNewGame.SetStyles(stlButton)
        btnNewGame.text = "New Game"
        btnNewGame.eventArgs = Controls.ID_frmNewGame
        btnNewGame.EventClick = AddressOf ChangeForm

        btnLoadGame.SetStyles(stlButton)
        btnLoadGame.y = -0.4
        btnLoadGame.text = "Load Game"
        btnLoadGame.eventArgs = Controls.ID_frmLoadGame
        btnLoadGame.EventClick = AddressOf ChangeForm

        btnPlayGame.SetStyles(stlButton)
        btnPlayGame.text = "Play Game"
        btnPlayGame.y = -0.4
        btnPlayGame.EventClick = AddressOf PlayGame

        btnRespawn.SetStyles(stlButton)
        btnRespawn.text = "Respawn"
        btnRespawn.y = -0.4
        btnRespawn.EventClick = AddressOf GameWorld.Respawn

        btnResume.SetStyles(stlButton)
        btnResume.text = "Continue"
        btnResume.y = -0.4
        btnResume.EventClick = AddressOf ChangeForm
        btnResume.eventArgs = -1

        btnMainMenuReturn.SetStyles(stlButton)
        btnMainMenuReturn.text = "Exit To Main Menu"
        btnMainMenuReturn.y = -0.7
        btnMainMenuReturn.EventClick = AddressOf ReturnToMainMenu
        btnMainMenuReturn.fontSize = 0.13
        btnMainMenuReturn.h = 0.13

        btnClose.SetStyles(stlButton)
        btnClose.SetBackgroundColour(New Colour("d11a2a"))
        btnClose.SetCoords(0.8, 0.8, 0.2, 0.2)
        btnClose.fontSize = 0.2
        btnClose.borderWidth = 0
        btnClose.text = "<Close>"
        btnClose.EventClick = AddressOf ExitProgram

        btnBack.SetStyles(stlButton)
        btnBack.SetBackgroundColour(New Colour("cccccc"))
        btnBack.transparentBackground = True
        btnBack.fontColour = New Colour("cccccc")
        btnBack.SetCoords(-1, 0.9, 0.15, 0.1)
        btnBack.fontSize = 0.1
        btnBack.borderWidth = 0
        btnBack.text = "<Left>"
        btnBack.EventClick = AddressOf ChangeFormBack
        btnBack.eventArgs = 0

        btnSettings.SetStyles(stlButton)
        btnSettings.transparentBackground = True
        btnSettings.text = "<Settings>"
        btnSettings.EventClick = AddressOf ChangeForm
        btnSettings.eventArgs = Controls.ID_frmSettings
        btnSettings.SetCoords(0.6, -0.8, 0.2, 0.2)

        txtSaveName.SetStyles(stlText)
        txtSaveName.y = 0.1
        txtSaveName.placeholder = "World Name"

        txtPassword.SetStyles(stlText)
        txtPassword.y = -0.1
        txtPassword.placeholder = "Password"

        '// Set IDs of controls
        btnClose.ID = Controls.ID_btnClose
        btnLoadGame.ID = Controls.ID_btnLoadGame
        btnNewGame.ID = Controls.ID_btnNewGame
        btnPlayGame.ID = Controls.ID_btnPlayGame
        txtSaveName.ID = Controls.ID_txtSaveName
        txtPassword.ID = Controls.ID_txtPassword

        Return formList.ToArray()
    End Function

    '// Empty subroutine as a placeholder for button subroutine invokes
    '// Prevents a button trying to call a subroutine at location 0 in memory
    Public Shared Sub ButtonTest(ByRef formArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
    End Sub

    '// Subroutine to be called on button click to change the current form
    Public Shared Sub ChangeForm(ByRef formArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        '// Add new form to the stack so the user can go back
        menuStack.Push(currentForm)
        currentForm = GetFormIndexByID(formArray, arguments)
        For i = 0 To formArray.Length - 1
            formArray(i).exitCondition = False
        Next
        '// Load the new form if not in the main game
        If currentForm <> -1 Then
            '// Try initialise the form if the delegate is non-zero
            If Not IsNothing(formArray(currentForm).InitialiseSubroutine) Then
                formArray(currentForm).InitialiseSubroutine(formArray, currentForm, arguments, sender)
            End If

            '// Call other initialisation subroutines for forms with specific IDs
            Select Case arguments
                Case Controls.ID_frmNewGame
                    NewGameInit(formArray(currentForm))
                Case Controls.ID_frmLoadGame
                    LoadGameInit(formArray(currentForm))
                    ChangeLoadGameItem(formArray, currentForm, 0, 0)
                Case Controls.ID_frmCrafting
                    GameWorld.character.craftingRecipes.SelectRecipe(formArray, currentForm, 0, -1)
                Case Controls.ID_frmCharacter
                    CraftingFormInit(formArray(currentForm))
                Case Controls.ID_frmMissions
                    MissionFormInit(formArray(currentForm))
                Case Controls.ID_frmInventory
                    GameWorld.character.inventoryItems.SelectItem(formArray, currentForm, 0, -1)
            End Select
        End If
    End Sub

    '// Initialise the load game form
    Private Shared Sub LoadGameInit(ByRef currentForm As Form)
        '// Get references to controls
        Dim password As New TextBoxPassword
        Dim playGame As New Button
        Dim errorMessage As New Label
        currentForm.GetElementByID(Controls.ID_txtPassword, password)
        currentForm.GetElementByID(Controls.ID_btnPlayGame, playGame)
        currentForm.GetElementByID(Controls.ID_lblInvalidReason, errorMessage)
        '// Clear game world and password fields
        errorMessage.text = ""
        playGame.enabled = True
        password.text = ""
        '// Refresh list of games in case some were created
        GameWorld.gamesToLoad = SaveGame.GetSavedGames()
    End Sub

    '// Initialise the new game form
    Private Shared Sub NewGameInit(ByRef currentForm As Form)
        '// Get references to controls
        Dim password As New TextBoxPassword
        Dim worldName As New TextBox
        Dim errorMessage As New Label
        currentForm.GetElementByID(Controls.ID_txtPassword, password)
        currentForm.GetElementByID(Controls.ID_txtSaveName, worldName)
        currentForm.GetElementByID(Controls.ID_lblInvalidReason, errorMessage)
        '// Clear control text
        password.text = ""
        worldName.text = ""
        errorMessage.text = ""
    End Sub

    '// Initialise mission form
    Private Shared Sub MissionFormInit(ByRef currentForm As Form)
        '// Refresh misison list
        GameWorld.character.missionList.LoadAllMissions()
        '// Deselect all forms
        GameWorld.character.missionList.SelectMission({currentForm}, 0, 0, -1)
    End Sub

    '// Initialise crafting form
    Private Shared Sub CraftingFormInit(ByRef currentForm As Form)
        '// Get references to controls
        Dim itemLabel As New Label
        Dim statLabel As New Label
        currentForm.GetElementByID(Controls.ID_lblCharacterItems, itemLabel)
        currentForm.GetElementByID(Controls.ID_lblCharacterStats, statLabel)
        '// Populate current target item details
        itemLabel.text = GameWorld.character.GetItems()
        statLabel.text = GameWorld.character.GetStats()
    End Sub

    '// Set current form to main menu
    Public Shared Sub ReturnToMainMenu(ByRef formArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        Dim firstForm As Integer
        '// Clear the stack to prevent stack overflow
        While menuStack.Pop(firstForm)
        End While
        '// Add main menu to the stack and set to current form
        currentForm = GetFormIndexByID(formArray, Controls.ID_frmMain)
        menuStack.Push(currentForm)
        For i = 0 To formArray.Length - 1
            formArray(i).exitCondition = False
        Next
        '// Reset load game controls
        If arguments = Controls.ID_frmLoadGame Then
            ChangeLoadGameItem(formArray, currentForm, 0, 0)
        End If
    End Sub

    Public Shared Function GetFormIndexByID(ByRef formArray As Form(), ID As Integer) As Integer
        '// Linear search through forms
        For i = 0 To formArray.Length - 1
            If formArray(i).ID = ID Then
                Return i
            End If
        Next
        Return -1
    End Function

    '// Load the previous form
    Public Shared Sub ChangeFormBack(ByRef formArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        menuStack.Pop(currentForm)
        For i = 0 To formArray.Length - 1
            formArray(i).exitCondition = False
        Next
        '// Add a delay to prevent the forms changing back repeatedly
        Threading.Thread.Sleep(100)
    End Sub

    '// Subroutine called when the play game buton is clicked
    '// Can load a game or create a new game
    Public Shared Sub PlayGame(ByRef frmArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        '// Get references to controls
        Dim txtSaveName As New Label
        Dim txtPassword As New Label
        Dim lblSaveName As New Label
        Dim lblInvalidReason As New Label
        Dim canLoad As Boolean = True

        frmArray(currentForm).GetElementByID(Controls.ID_txtSaveName, txtSaveName)
        frmArray(currentForm).GetElementByID(Controls.ID_txtPassword, txtPassword)
        frmArray(currentForm).GetElementByID(Controls.ID_lblLoadGameName, lblSaveName)
        frmArray(currentForm).GetElementByID(Controls.ID_lblInvalidReason, lblInvalidReason)

        Select Case frmArray(currentForm).ID
            '// Create a new game
            Case Controls.ID_frmNewGame
                If SaveGame.Duplicate(txtSaveName.text) Then
                    '// Name is already used
                    lblInvalidReason.text = "File name must be unique"
                    canLoad = False
                ElseIf Not SaveGame.StrongPassword(txtPassword.text, lblInvalidReason.text) Then
                    '// Password is insufficient
                    canLoad = False
                Else
                    '// Create a new game and load it
                    SaveGame.CreateGame(txtSaveName.text, txtPassword.text, GameWorld.character, GameWorld.timeOfDay, GameWorld.NPCs)
                    SaveGame.LoadGame(txtSaveName.text, GameWorld.character, GameWorld.timeOfDay, GameWorld.NPCs)

                    GameWorld.saveFile = txtSaveName.text
                End If
            '// Load an existing game
            Case Controls.ID_frmLoadGame
                If SaveGame.CorrectPassword(lblSaveName.text, txtPassword.text) Then
                    '// Load the game
                    SaveGame.LoadGame(lblSaveName.text, GameWorld.character, GameWorld.timeOfDay, GameWorld.NPCs)
                    GameWorld.saveFile = lblSaveName.text
                Else
                    '// Password hash does not match stored password
                    lblInvalidReason.text = "Password is incorrect"
                    canLoad = False
                End If
        End Select
        '// If loaded then set the game mode to playing a game
        If canLoad Then GameWorld.activeForm = -1

    End Sub

    '// Enum to provide meaningful ID values that can be looked up
    Public Enum Controls
        ID_btnClose = 0
        ID_btnLoadGame = 1
        ID_btnNewGame = 2
        ID_btnPlayGame = 3
        ID_txtSaveName = 4
        ID_txtPassword = 5
        ID_frmMain = 6
        ID_frmNewGame = 7
        ID_frmLoadGame = 8
        ID_frmGameOver = 9
        ID_frmInventory = 10
        ID_frmCharacter = 11
        ID_frmCrafting = 12
        ID_frmMissions = 13
        ID_lblLoadGameName = 14
        ID_grdInventory = 15
        ID_lblItemDetail = 16
        ID_renItemDetailImage = 17
        ID_btnEquipItem = 18
        ID_renCharacter = 19
        ID_frmSettings = 20
        ID_frmSettingsInput = 21
        ID_frmSettingsGraphics = 22
        ID_frmGameplay = 23
        ID_frmPaused = 24
        ID_cmbFilter = 25
        ID_cmbSort = 26
        ID_grdCrafting = 27
        ID_lblRecipeDetail = 28
        ID_renRecipeDetailImage = 29
        ID_btnCraftRecipe = 30
        ID_lblInvalidReason = 31
        ID_lblCharacterStats = 32
        ID_lblCharacterItems = 33
        ID_lstMissions = 34
        ID_lstTasks = 35
        ID_frmTalk = 36
        ID_lblTalk = 37
    End Enum

End Class

