Option Strict On
Imports NEA.OpenGLImporter
Imports NEA.OpenGLWrapper
Imports System.Runtime.InteropServices
Imports NEA

'// Contains a group of classes for controls and GUI
Public Class GUIObject

    Const StylePath As String = "Resources\Styles\"
    Const MOUSE_SIZE As Single = 0.1

    '// GUI control representing a vertical button list
    Public Class TaskList
        Inherits Button
        '// Multiple copies of the base are rendered according to the data in the lists
        Public taskItemBase As TaskItem
        Private taskNames As New List(Of String)
        Private taskProgress As New List(Of Integer)
        Private taskTarget As New List(Of Integer)
        Private taskShowProgress As New List(Of Boolean)
        Private selectedIndex As Integer

        Public Overrides Sub OnHover(collision As Integer)
            selectedIndex = collision
        End Sub

        Public Overrides Sub OnClick(collision As Integer)
            MyBase.OnClick(selectedIndex)
        End Sub

        '// Check for which option the mouse is over
        Public Overrides Function MouseCollision(mouseCoord As Form.COORD) As Integer
            For i = 0 To taskNames.Count - 1
                LoadTaskInstance(i)
                If taskItemBase.MouseCollision(mouseCoord) > -1 Then
                    Return i
                End If
            Next
            selectedIndex = -1
            Return -1
        End Function

        '// Clear all lists
        Public Sub ClearTask()
            taskNames.Clear()
            taskProgress.Clear()
            taskTarget.Clear()
            taskShowProgress.Clear()
        End Sub

        '// Add task details to the lists
        Public Sub SetTask(name As String, progress As Integer, target As Integer, showProgress As Boolean)
            taskNames.Add(name)
            taskProgress.Add(progress)
            taskTarget.Add(target)
            taskShowProgress.Add(showProgress)
        End Sub

        '// Add coordinates of all elements to the list
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            '// Display background container
            newIndex = MyBase.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            For i = 0 To taskNames.Count - 1
                '// Fill button with specific data
                LoadTaskInstance(i)
                '// Change background colour based on whether it is clicked
                If selectedIndex = i Then
                    taskItemBase.SetBackgroundColour(New Colour("FFFFFF"))
                Else
                    taskItemBase.SetBackgroundColour(New Colour("FFFEDE"))
                End If
                newIndex = taskItemBase.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            Next
            Return newIndex
        End Function

        '// Display text of all buttons
        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            newIndex = MyBase.AddTextCoordsToArray(position, texture, colour, newIndex, text, textureMap, topZPass)
            For i = 0 To taskNames.Count - 1
                LoadTaskInstance(i)
                newIndex = taskItemBase.AddTextCoordsToArray(position, texture, colour, newIndex, taskNames(i), textureMap, topZPass)
            Next
            Return newIndex
        End Function

        '// Set specific details of the button for the instance
        '// Set progress bar, display flag and coordinates
        Private Sub LoadTaskInstance(index As Integer)
            taskItemBase.SetCoords(x + w * 0.01F, y + h * 0.9F - h * 0.1F * index, w * 0.98F, 0.098F * h)
            taskItemBase.progressBar.barMaxValue = taskTarget(index)
            taskItemBase.progressBar.currentValue = taskProgress(index)
            taskItemBase.showbar = taskShowProgress(index)
        End Sub

    End Class

    '// Represents a button with a progress bar
    Public Class TaskItem
        Inherits Button
        Public progressBar As Bar
        Public showbar As Boolean
        Sub New(barColour As Colour)
            progressBar = New Bar(barColour)
        End Sub

        '// Conditionally add a tick if the task is complete
        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            If progressBar.currentValue = progressBar.barMaxValue Then
                Return MyBase.AddTextCoordsToArray(position, texture, colour, index, text & "  <#00FF00><Tick>", textureMap, topZPass)
            Else
                Return MyBase.AddTextCoordsToArray(position, texture, colour, index, text, textureMap, topZPass)
            End If
        End Function

        '// Add background colours to array
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            newIndex = MyBase.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            If progressBar.currentValue <> progressBar.barMaxValue And showbar Then
                '// If the bar should be shown and is not replaced by a tick, show it
                newIndex = progressBar.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            End If
            Return newIndex
        End Function

        '// Ensure the progress bar is always in the same place relative to the control
        Public Overrides Sub SetCoords(inX As Single, inY As Single, inW As Single, inH As Single)
            MyBase.SetCoords(inX, inY, inW, inH)
            progressBar.SetCoords(x + w * 0.65F, y + h * 0.5F, w * 0.3F, h * 0.4F)
        End Sub
    End Class

    '// Represents multiple bars
    '// Used to display multiple enemy health bars
    Public Class HealthBarCollection
        Inherits Label
        '// There is a base bar control with lists of instance data
        Public healthBar As Bar
        Public healthPositions As New List(Of CoordDataTypes.COORD3Sng)
        Public healthMaxValues As New List(Of Integer)
        Public healthValues As New List(Of Integer)
        Public worldSpaceTransform As Matrices

        Sub New(barColour As Colour)
            healthBar = New Bar(barColour)
        End Sub

        '// Adds all background and borders to the buffer
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index

            '// Iterate through all health bars and display them if they should be visible
            For i = 0 To healthPositions.Count - 1
                If SetPosition(healthBar, healthPositions(i)) Then
                    '// Load specific data for the instance
                    healthBar.barMaxValue = healthMaxValues(i)
                    healthBar.currentValue = healthValues(i)
                    '// Add data to array
                    newIndex = healthBar.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
                End If
            Next
            Return newIndex
        End Function

        '// Converts the position of the bar from world space to screen space
        '// Returns true if the bar is in front of the screen
        Private Function SetPosition(ByRef toSet As Bar, worldSpace As CoordDataTypes.COORD3Sng) As Boolean
            Dim worldSpaceMatrix As New Matrices(4, 1, False)
            Dim screenSpace As Matrices
            Dim size As New CoordDataTypes.COORD2Sng(0.4, 0.07)
            Dim location As New CoordDataTypes.COORD2Sng(0, 0)

            '// Project coordinates to screen
            worldSpaceMatrix.data = {worldSpace.x, worldSpace.y, worldSpace.z, 1}
            screenSpace = Matrices.Multiply(worldSpaceTransform, worldSpaceMatrix)

            '// Scale by z coordinate
            size.x /= screenSpace.data(3)
            size.y /= screenSpace.data(3)
            location.x = screenSpace.data(0) / screenSpace.data(3)
            location.y = screenSpace.data(1) / screenSpace.data(3)

            '// Center the bar
            location.x -= size.x * 0.5F
            location.y -= size.y * 0.5F

            '// Set the coordinates and size
            toSet.SetCoords(location.x, location.y, size.x, size.y)
            toSet.borderWidth = size.x / 20

            '// Only draw the bar if it is in front of you
            Return screenSpace.data(3) > 0
        End Function
    End Class

    '// Represents a bar whose size can be altered
    Public Class Bar
        Inherits Label
        Private ProgressPart As Label
        Private barValue As Integer
        Public barMaxValue As Integer

        '// Initialise bar and create necessary labels
        Sub New(barColour As Colour)
            SetBackgroundGradient(New Colour(0.1, 0.1, 0.1), New Colour(0.3, 0.3, 0.3))
            ProgressPart = New Label
            ProgressPart.SetBackgroundGradient(New Colour(barColour.r * 1.3F, barColour.g * 1.3F, barColour.b * 1.3F), New Colour(barColour.r * 0.7F, barColour.g * 0.7F, barColour.b * 0.7F))
            SetBorderColour(New Colour("000000"))
            borderWidth = 0.01
        End Sub

        '// Ensure all children are in the same position relative to the control
        Public Overrides Sub SetCoords(inX As Single, inY As Single, inW As Single, inH As Single)
            MyBase.SetCoords(inX, inY, inW, inH)
            ProgressPart.SetCoords(inX, inY, CSng(barValue / barMaxValue * w), inH)
        End Sub

        Public Property currentValue As Integer
            Get
                Return barValue
            End Get
            Set(value As Integer)
                '// Make sure the value does not go out of range
                barValue = Math.Min(Math.Max(value, 0), barMaxValue)
                ProgressPart.w = CSng(barValue / barMaxValue * w)
            End Set
        End Property

        '// Bypass the text as bars do not have text
        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            Return index
        End Function

        '// Ensure all bars are drawn
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            newIndex = MyBase.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            newIndex = ProgressPart.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            Return newIndex
        End Function
    End Class

    '// Class for a control whose state can be toggled when clicked
    '// Adds a caption to the toggle button class
    Public Class ToggleButtonLabel
        Inherits Label
        Public toggle As ToggleButton
        Public Overrides Property borderWidth As Single
            Get
                Return MyBase.borderWidth
            End Get
            Set(value As Single)
                MyBase.borderWidth = value
                toggle.borderWidth = value
            End Set
        End Property

        '// Initialise a new toggle button whose states are ON (green) and OFF (red)
        Public Sub New(ByRef graphicsBinding As GameWorld.GraphicsSettings)
            toggle = New ToggleButton({"OFF", "ON"}, {New Colour("ff0000"), New Colour("00ff00")})
            toggle.fontSize = 0.1
            toggle.toggleState = 0
            toggle.graphicsSettings = graphicsBinding
        End Sub

        '// Ensure children are in the same position relative to the control
        Public Overrides Sub SetCoords(inX As Single, inY As Single, inW As Single, inH As Single)
            MyBase.SetCoords(inX, inY, inW, inH)
            toggle.SetCoords(inX + inW - 0.2F, inY, 0.2, inH)
        End Sub

        '// Add all background colours to the array
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            newIndex = MyBase.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            newIndex = toggle.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            Return newIndex
        End Function

        '// Ensure all text is added to the buffer
        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            newIndex = MyBase.AddTextCoordsToArray(position, texture, colour, newIndex, text, textureMap, topZPass)
            newIndex = toggle.AddTextCoordsToArray(position, texture, colour, newIndex, toggle.text, textureMap, topZPass)
            Return newIndex
        End Function

        '// Pass the click event to the toggle button so it can be toggled
        Public Overrides Sub OnClick(collision As Integer)
            MyBase.OnClick(collision)
            toggle.OnClick(collision)
        End Sub
    End Class

    '// Represents a button that can toggle between two states
    Public Class ToggleButton
        Inherits Label
        Private toggleStateHidden As Integer
        Public graphicsSettings As GameWorld.GraphicsSettings
        Public graphicsSettingsIndex As Integer
        '// Ensure that the background colour corresponds to the current state
        Public Property toggleState As Integer
            Get
                toggleStateHidden = graphicsSettings.GetGraphicsSettings(graphicsSettingsIndex)
                SetBackgroundGradient(New Colour("ffffff"), toggleColours(toggleStateHidden))
                Return toggleStateHidden
            End Get
            Set(value As Integer)
                toggleStateHidden = value Mod toggleValues.Length
                If Not IsNothing(graphicsSettings) Then
                    graphicsSettings.SetGraphicsSettings(graphicsSettingsIndex, toggleStateHidden)
                End If
                SetBackgroundGradient(New Colour("ffffff"), toggleColours(toggleStateHidden))
            End Set
        End Property
        Public toggleValues As String()
        Public toggleColours As Colour()
        '// The text returned depends on the current state
        Public Overrides Property text As String
            Get
                Return toggleValues(toggleState)
            End Get
            Set(value As String)
                MyBase.text = value
            End Set
        End Property
        Sub New(inToggleValues As String(), inToggleColours As Colour())
            toggleValues = inToggleValues
            toggleColours = inToggleColours
        End Sub
        '// Toggle the current state when clicked
        Public Overrides Sub OnClick(collision As Integer)
            MyBase.OnClick(collision)
            toggleState += 1
        End Sub
    End Class

    '// GUI control to wait for a key press and set state to the keypress
    Public Class KeyBind
        Inherits Label
        '// References to key binding objects
        Private keyBinding As KeyboardInput.KeyBinding
        Private keyToBind As KeyboardInput.KeyBinds

        '// Set references and provide an identifier to the input this represents
        Sub New(inKeyBinding As KeyboardInput.KeyBinding, inKeyToBind As KeyboardInput.KeyBinds)
            keyBinding = inKeyBinding
            keyToBind = inKeyToBind
            character = Chr(keyBinding.GetKeyBinds(keyToBind))
        End Sub

        Public Overrides Property text As String
            Get
                If selected Then
                    '// Change display to let he user know this one can be changed
                    Return baseText & " " & "<QuestionMark>"
                Else
                    Return baseText & " " & KeyboardInput.GetSpecialCharacter(character)
                End If
            End Get
            Set(value As String)
                baseText = value
            End Set
        End Property
        Private baseText As String
        Private Property character As String
            Get
                Return Chr(keyBinding.GetKeyBinds(keyToBind))
            End Get
            Set(value As String)
                '// Ensure value of the target object is also set
                keyBinding.SetKeyBinds(keyToBind, Asc(value(0)))
            End Set
        End Property

        Public Overrides Sub KeyPress(keys() As Integer)
            Dim firstKeyPressed As Integer = keys(0)
            If firstKeyPressed >= Asc("a"c) And firstKeyPressed <= Asc("z"c) Then
                '// Make it upper case
                firstKeyPressed = firstKeyPressed And Not 32
            End If

            '// Set current key to the key pressed
            If firstKeyPressed > 10 Then
                MyBase.KeyPress(keys)
                character = Chr(firstKeyPressed)
                keyBinding.SetKeyBinds(keyToBind, firstKeyPressed)
                OnDeclick(0)
            End If
        End Sub

        '// Change font colour based on toggle to show if it selected
        Public Overrides Sub OnClick(collision As Integer)
            MyBase.OnClick(collision)
            fontColour = New Colour("f0f000")
        End Sub

        Public Overrides Sub OnDeclick(collision As Integer)
            MyBase.OnDeclick(collision)
            fontColour = New Colour("000000")
        End Sub

    End Class

    '// Control in which a GLTF model can be rendered
    Public Class RenderTarget
        Inherits Label
        Public model As Mob
        Public GLTFProgram As UInteger
        Public screen As CoordDataTypes.COORD2Short
        Private viewportPosition As CoordDataTypes.COORD2Short
        Private viewportSize As CoordDataTypes.COORD2Short
        Public position As CoordDataTypes.COORD3Sng
        Public modelElevation As Single
        Public rotate As Boolean
        Private context As OpenGLContext

        Sub New(ByRef inContext As OpenGLContext)
            context = inContext
            controlType = Controls.RenderTarget
            screen = Window.GetSize()
        End Sub

        '// Ensure the render viewport is the same position and size as the control
        Public Overrides Sub SetCoords(inX As Single, inY As Single, inW As Single, inH As Single)
            MyBase.SetCoords(inX, inY, inW, inH)
            viewportPosition.x = CShort((x + 1) / 2 * screen.x)
            viewportPosition.y = CShort((y + 1) / 2 * screen.y)
            viewportSize.x = CShort(w / 2 * screen.x)
            viewportSize.y = CShort(h / 2 * screen.y)
        End Sub

        '// Render the 3D object to the screen
        Public Sub DisplayModel(lightSource As CoordDataTypes.COORD3Sng)
            Dim matrixView As New Matrices(4, 4, True)
            Dim matrixRelative As New Matrices(4, 4, True)
            Dim matrixPerspective As New Matrices(4, 4, True)
            Dim oldPosition As New CoordDataTypes.COORD3Sng
            Dim oldRotation As Single

            '// Generate matrices for a viewer directly in front of the object
            matrixRelative = MatrixGenerator.GetRelativeMatrix(position.x, position.y, position.z)
            matrixView = MatrixGenerator.GetViewMatrix(matrixRelative, 0, 0)
            matrixPerspective = MatrixGenerator.GetPerspectiveMatrix(matrixView, viewportSize)

            context.glUseProgram(GLTFProgram)
            glUniform3f(glGetUniformLocationStr(CInt(GLTFProgram), "light"), lightSource.x, lightSource.y, lightSource.z)
            '// Backup original positions and locations of model
            oldPosition.x = model.location.x
            oldPosition.y = model.location.y
            oldPosition.z = model.location.z
            oldRotation = model.actualRotation
            '// Ensure the model is in front of the camera
            model.location = New CoordDataTypes.COORD3Sng(0, 0, 0)
            model.actualRotation = CSng(Math.PI)
            model.UpdateMatrices()
            '// Spin the model around
            If rotate Then model.actualRotation = CSng(Timer / 2)

            model.elevation = modelElevation

            '// Set the viewport so OpenGL only renders where the control is
            glViewport(viewportPosition.x, viewportPosition.y, viewportSize.x, viewportSize.y)
            glEnable(GL_DEPTH_TEST)
            glClearDepth(1)
            glClear(GL_DEPTH_BUFFER_BIT)
            glDepthFunc(GL_LESS)

            '// Set to default animation
            model.animationName = "walk"
            model.animationList.Clear()
            model.animationProgress = CSng(Timer)
            '// Render model
            model.Display(GLTFProgram, matrixView, matrixRelative, matrixPerspective, False, {New Matrices(4, 4, True)}, -1)
            '// Restore the position of the model
            model.location.x = oldPosition.x
            model.location.y = oldPosition.y
            model.location.z = oldPosition.z
            model.actualRotation = oldRotation
        End Sub

    End Class

    '// Control showing a grid of selectable items
    Public Class GridView
        Inherits Label
        Public items(15) As CaptionedPicture
        Public itemCount As Integer
        Public btnNext As New Button
        Public btnPrev As New Button
        Public currentPage As Integer = 0
        Public savedMouseCoord As Form.COORD
        Public pageInvalid As Boolean
        Public firstPage As Boolean
        Public lastPage As Boolean
        Public Sub SetProgram(program As UInteger)
            For i = 0 To items.Length - 1
                items(i).image.GLTFProgram = program
            Next
        End Sub
        Sub New(buttonFormat As StyleSheet, captionFormat As StyleSheet, imageFormat As StyleSheet, ByRef context As OpenGLContext)
            '// Initialise next and previous buttons
            btnNext.SetStyles(buttonFormat)
            btnPrev.SetStyles(buttonFormat)
            btnNext.text = "<Right>"
            btnPrev.text = "<Left>"
            btnNext.transparentBackground = True
            btnPrev.transparentBackground = True
            btnNext.EventClick = AddressOf ChangePage
            btnPrev.EventClick = AddressOf ChangePage
            btnNext.eventArgs = 1
            btnPrev.eventArgs = -1
            '// Initialise grid items
            For i = 0 To items.Length - 1
                items(i) = New CaptionedPicture(context)
                items(i).SetStyles(imageFormat)
                items(i).caption.SetStyles(captionFormat)
                items(i).additionalInfo.SetStyles(captionFormat)
                items(i).additionalInfo.transparentBackground = True
                items(i).additionalInfo.borderWidth = 0
                items(i).EventClick = AddressOf Menu.ButtonTest
                items(i).eventArgs = i
                items(i).image.modelElevation = CSng(Math.PI / 4)
                items(i).image.position = New CoordDataTypes.COORD3Sng(0, 0, -0.5)
            Next
            skipHover = True
            controlType = Controls.GridView
        End Sub

        '// Event called when either the next or previous button is clicked
        Public Sub ChangePage(ByRef formArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
            currentPage += arguments
            '// Set a flag to refresh the item contents
            pageInvalid = True
        End Sub

        Public Overrides Sub OnClick(collision As Integer)
            MyBase.OnClick(collision)
            For i = 0 To itemCount - 1
                '// Check if a grid element was clicked
                collision = items(i).MouseCollision(savedMouseCoord)
                If collision > -1 Then
                    items(i).OnClick(collision)
                End If
            Next
            '// Check if a navigation button was clicked
            If btnPrev.MouseCollision(savedMouseCoord) > -1 Then btnPrev.OnClick(collision)
            If btnNext.MouseCollision(savedMouseCoord) > -1 Then btnNext.OnClick(collision)
        End Sub

        '// Apply hover effects to elements individually
        Public Overrides Sub OnHover(collision As Integer)
            Dim oldHover As Integer

            For i = 0 To itemCount - 1
                oldHover = items(i).hover
                collision = items(i).MouseCollision(savedMouseCoord)
                If collision <> oldHover Then
                    items(i).OnDehover()
                    If collision > -1 Then
                        items(i).OnHover(collision)
                    End If
                End If
            Next

            OnHoverButtons(btnNext)
            OnHoverButtons(btnPrev)
        End Sub
        Private Sub OnHoverButtons(ByRef buttonCheck As Button)
            Dim oldHover As Integer
            Dim collision As Integer

            oldHover = buttonCheck.hover
            collision = buttonCheck.MouseCollision(savedMouseCoord)
            If collision <> oldHover Then
                buttonCheck.OnDehover()
                If collision > -1 Then
                    buttonCheck.OnHover(collision)
                End If
            End If
        End Sub
        Public Overrides Function MouseCollision(mouseCoord As Form.COORD) As Integer
            Dim collision As Integer
            '// Allows the coordinates to be further tested for the individual items.
            savedMouseCoord = mouseCoord
            collision = MyBase.MouseCollision(mouseCoord)
            '// Should always check, even if it has not been toggled as it has children.
            OnHover(collision)
            Return collision
        End Function

        '// Ensure that children are in the same relative location to the control when the control is moved
        Public Sub SetAllCoords(x As Single, y As Single, w As Single, h As Single)
            SetCoords(x, y, w, h)
            SetChildrenCoords()
        End Sub
        Private Sub SetChildrenCoords()
            SetChildCoords(btnPrev, 0.08, 0.05, 0.15, 0.1)
            SetChildCoords(btnNext, 0.83, 0.05, 0.15, 0.1)

            For i = 0 To items.Length - 1
                SetChildCoords(items(i), (i Mod 4) * 0.25F + 0.08F, 0.8F - (i \ 4) * 0.2F, 0.15F, 0.15F)
            Next
        End Sub
        Private Sub SetChildCoords(ByRef target As Button, relativeX As Single, relativeY As Single, relativeW As Single, relativeH As Single)
            target.SetCoords(x + relativeX * w, y + relativeY * h, relativeW * w, relativeH * h)
        End Sub
        Private Sub SetChildCoords(ByRef target As CaptionedPicture, relativeX As Single, relativeY As Single, relativeW As Single, relativeH As Single)
            target.SetCoords(x + relativeX * w, y + relativeY * h, relativeW * w, relativeH * h)
            target.SetPosition(x + relativeX * w, y + relativeY * h)
        End Sub

        '// Ensure all children are displayed
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            newIndex = MyBase.AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            For i = 0 To itemCount - 1
                newIndex = items(i).AddBorderCoordsToArray(position, texture, colour, newIndex, topZPass)
            Next
            Return newIndex
        End Function
        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            Dim newIndex As Integer = index
            newIndex = MyBase.AddTextCoordsToArray(position, texture, colour, newIndex, text, textureMap, topZPass)
            If Not lastPage Then
                newIndex = btnNext.AddTextCoordsToArray(position, texture, colour, newIndex, btnNext.text, textureMap, topZPass)
            End If
            If Not firstPage Then
                newIndex = btnPrev.AddTextCoordsToArray(position, texture, colour, newIndex, btnPrev.text, textureMap, topZPass)
            End If
            For i = 0 To itemCount - 1
                newIndex = items(i).AddTextCoordsToArray(position, texture, colour, newIndex, items(i).text, textureMap, topZPass)
            Next
            Return newIndex
        End Function
    End Class

    '// Control that represents a 3D model with a caption
    Public Class CaptionedPicture
        Inherits Button
        Public additionalInfo As New Label
        Public caption As New Label
        Public image As RenderTarget
        Public captionFraction As Single = 0.3
        Sub New(ByRef context As OpenGLContext)
            image = New RenderTarget(context)
            image.rotate = False
            image.SetBackgroundColour(New Colour("FFFFFF"))
            '// The captions are displayed above the image
            additionalInfo.topZ = True
            caption.topZ = True
            additionalInfo.transparentBackground = True
            additionalInfo.borderWidth = 0
        End Sub

        '// Keep the relative position of the children the same when the whole control is moved
        Public Sub SetPosition(posX As Single, posY As Single)
            SetCoords(posX, posY, w, h)
            caption.SetCoords(posX, posY, w, h * captionFraction)
            additionalInfo.SetCoords(posX + w * (1 - captionFraction), posY + h * (1 - captionFraction), w * captionFraction, h * captionFraction)
            image.SetCoords(posX, posY + h * captionFraction, w, h * (1 - captionFraction))
        End Sub

        '// Ensure all children are displayed
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            Dim numCoords As Integer = index
            numCoords = MyBase.AddBorderCoordsToArray(position, texture, colour, numCoords, topZPass)
            numCoords = caption.AddBorderCoordsToArray(position, texture, colour, numCoords, topZPass)
            If additionalInfo.text <> "" Then
                '// Do not show the additional information if it is blank
                numCoords = additionalInfo.AddBorderCoordsToArray(position, texture, colour, numCoords, topZPass)
            End If
            numCoords = image.AddBorderCoordsToArray(position, texture, colour, numCoords, topZPass)
            Return numCoords
        End Function
        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            Dim numCoords As Integer = index
            numCoords = MyBase.AddTextCoordsToArray(position, texture, colour, numCoords, text, textureMap, topZPass)
            numCoords = caption.AddTextCoordsToArray(position, texture, colour, numCoords, caption.text, textureMap, topZPass)
            numCoords = additionalInfo.AddTextCoordsToArray(position, texture, colour, numCoords, additionalInfo.text, textureMap, topZPass)
            Return numCoords
        End Function
    End Class

    '// Represents a collection of controls that can be displayed
    Public Class Form
        Public transparentBackground As Boolean = False
        Public children As New List(Of Label)
        Public mouse As New Label
        Public exitCondition As Boolean
        Public exitSubroutine As Button.EventClickDel
        Public arguments As Integer
        Delegate Sub FormUpdate(frm As Form)
        Public UpdateSubroutine As FormUpdate
        Public InitialiseSubroutine As Button.EventClickDel
        Public ID As Integer
        Sub New()
            exitCondition = False
            '// Initialise controlto display the mouse cursor
            mouse.text = "<Mouse>"
            mouse.transparentBackground = True
            mouse.borderWidth = 0
            mouse.fontSize = MOUSE_SIZE
            mouse.fontColour = New Colour("4C3200")
            mouse.topZ = True
            UpdateSubroutine = AddressOf Menu.FormUpdateDefault
        End Sub

        '// Update subroutine is sometimes used as a listener for keypresses depending on the form
        Public Sub UpdateForm()
            UpdateSubroutine.Invoke(Me)
        End Sub

        '// Linear search for child controls
        '// There are many overloads as there are different types of controls that need to be found

        Public Function GetElementByID(ID As Integer, ByRef element As Label) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = children(i)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetElementByID(ID As Integer, ByRef element As Button) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = CType(children(i), Button)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetElementByID(ID As Integer, ByRef element As TextBox) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = CType(children(i), TextBox)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetElementByID(ID As Integer, ByRef element As TextBoxPassword) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = CType(children(i), TextBoxPassword)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetElementByID(ID As Integer, ByRef element As RenderTarget) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = CType(children(i), RenderTarget)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetElementByID(ID As Integer, ByRef element As DropDown) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = CType(children(i), DropDown)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetElementByID(ID As Integer, ByRef element As TaskList) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = CType(children(i), TaskList)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetElementByID(ID As Integer, ByRef element As GridView) As Boolean
            For i = 0 To children.Count - 1
                If children(i).ID = ID Then
                    element = CType(children(i), GridView)
                    Return True
                End If
            Next
            Return False
        End Function

        '// Add a new control and add a parent reference to the control
        Sub NewChild(childToAdd As Label)
            children.Add(childToAdd)
            childToAdd.parent.Add(Me)
        End Sub

        '// Populate buffers containing the data to be displayed
        Public Function GenerateVertexData(ByRef position As Single(), ByRef texture As Single(), ByRef colour As Single(), mousePos As COORD, textureMap As TextureLoader.TextureMap, displayMouse As Boolean, topZPass As Boolean) As Integer
            Dim GUIIndex As Integer = 0
            If topZPass And ID = Menu.Controls.ID_frmInventory Then
                GUIIndex = 0
            End If
            '// Iterate through all children and add position, colour and texture data
            For i = 0 To children.Count - 1
                If children(i).visible Then
                    '// Conditionally display the background
                    If Not children(i).transparentBackground Then
                        GUIIndex = children(i).AddBorderCoordsToArray(position, texture, colour, GUIIndex, topZPass)
                    End If
                    GUIIndex = children(i).AddTextCoordsToArray(position, texture, colour, GUIIndex, children(i).text, textureMap, topZPass)
                End If
            Next
            '// Add another label with the mouse cursor
            If displayMouse Then
                mouse.SetCoords(mousePos.x, mousePos.y - MOUSE_SIZE, MOUSE_SIZE, MOUSE_SIZE)
                GUIIndex = mouse.AddTextCoordsToArray(position, texture, colour, GUIIndex, mouse.text, textureMap, topZPass)
            End If
            Return GUIIndex
        End Function

        '// Handle mouse click events
        Public Sub MouseClick(mouseCoord As COORD)
            Dim collision As Integer
            Dim clicked(children.Count - 1) As Boolean
            Dim anyClick As Boolean
            For i = 0 To children.Count - 1
                '// Check each child for a collision
                '// This occurs if the mouse is hovering over the control
                collision = children(i).MouseCollision(mouseCoord)
                If collision > -1 Then
                    clicked(i) = True
                    anyClick = True
                    '// Allow the control to deal with the mosue click event and perform actions
                    If children(i).enabled Then children(i).OnClick(collision)
                End If
            Next
            '// Reset toggle states of children if not clicked on
            For i = 0 To children.Count - 1
                If Not clicked(i) And children(i).selected Then
                    If children(i).enabled Then children(i).OnDeclick(collision)
                End If
            Next
        End Sub

        '// Allow children (mainly text boxes) to handle keyboard input
        Public Sub KeyPressed(KeyPressed As Integer())
            For i = 0 To children.Count - 1
                If children(i).selected Then
                    children(i).KeyPress(KeyPressed)
                End If
            Next
        End Sub

        '// Provide animations of controls when the mouse hovers over them
        Public Sub MouseHover(MouseCoord As COORD)
            Dim collision As Integer
            Dim oldHover As Integer
            For i = 0 To children.Count - 1
                oldHover = children(i).hover
                collision = children(i).MouseCollision(MouseCoord)
                If collision <> oldHover Then
                    '// Return to default state
                    children(i).OnDehover()
                End If
                If collision > -1 Then
                    '// If enabled, change control state to provide visual feedback to user
                    If collision <> oldHover And children(i).enabled Then
                        children(i).OnHover(collision)
                    End If
                End If
            Next
        End Sub

        Structure COORD
            Public x As Single
            Public y As Single
        End Structure
    End Class

    '// Represents an RGB colour value
    Public Structure Colour
        Public r As Single
        Public g As Single
        Public b As Single
        Const HexToDec As String = "0123456789ABCDEF"

        '// Multiple contructors

        '// Normalised RGB values 0.0 to 1.0
        Sub New(inR As Single, inG As Single, inB As Single)
            r = inR
            g = inG
            b = inB
        End Sub

        '// Byte RGB values 0 to 255
        Sub New(inR As Byte, inG As Byte, inB As Byte)
            r = CSng(inR) / 255
            g = CSng(inG) / 255
            b = CSng(inB) / 255
        End Sub

        '// Hexadecimal colour code
        Sub New(hexValue As String)
            hexValue = hexValue.ToUpper()
            r = GetValue(hexValue(0), hexValue(1))
            g = GetValue(hexValue(2), hexValue(3))
            b = GetValue(hexValue(4), hexValue(5))
        End Sub

        '// Convert 2 character hex to normalised float
        Private Function GetValue(MSB As Char, LSB As Char) As Single
            Return CSng((HexToDec.IndexOf(MSB) * 16 + HexToDec.IndexOf(LSB)) / 255)
        End Function

        '// Linear interpolate between two colours
        Public Shared Function Lerp(colA As Colour, colB As Colour, strength As Single) As Colour
            Return New Colour(colA.r * strength + colB.r * (1 - strength), colA.g * strength + colB.g * (1 - strength), colA.b * strength + colB.b * (1 - strength))
        End Function

        '// Scale colours by a constant
        Public Shared Operator *(ByVal col As Colour, ByVal scalar As Single) As Colour
            Return New Colour(col.r * scalar, col.g * scalar, col.b * scalar)
        End Operator
    End Structure

    '// Control representing text, border and a background
    '// Provides a base class for most other controls
    Public Class Label
        Public topZ As Boolean
        Public ID As Integer
        Public x As Single
        Public y As Single
        Public w As Single
        Public h As Single
        Public Overridable Property text As String = ""
        Public fontSize As Single
        Public Overridable Property borderWidth As Single
        Protected borderColour(3) As Colour
        Protected backgroundColour(3) As Colour
        Public fontColour As Colour
        Public hover As Integer
        Public selected As Boolean
        Public parent As New List(Of Form)
        Public enabled As Boolean = True
        Public transparentBackground As Boolean = False
        Public skipHover As Boolean = False
        Public controlType As Controls = Controls.Label
        Public visible As Boolean = True

        '// Copy styles from a stylesheet
        Public Sub SetStyles(style As StyleSheet)
            x = style.x
            y = style.y
            w = style.w
            h = style.h
            text = style.text
            fontSize = style.fontSize
            borderWidth = style.borderWidth
            For i = 0 To 3
                borderColour(i) = style.borderColour(i)
                backgroundColour(i) = style.backgroundColour(i)
            Next
            fontColour = style.fontColour
            transparentBackground = style.transparentBackground
        End Sub

        '// Default actions to be overridden
        Public Overridable Sub OnClick(collision As Integer)
            selected = True
        End Sub
        Public Overridable Sub OnHover(collision As Integer)
            If Not skipHover Then
                hover = 0
                borderWidth *= 2
            End If
        End Sub
        Public Overridable Sub OnDehover()
            If Not skipHover Then
                If hover = 0 Then
                    hover = -1
                    borderWidth /= 2
                End If
            End If
        End Sub
        Public Overridable Sub OnDeclick(collision As Integer)
            selected = False
        End Sub
        Public Overridable Sub KeyPress(keys As Integer())

        End Sub
        Public Overridable Function MouseCollision(mouseCoord As Form.COORD) As Integer
            '// Check if mouse is within the bounds of the control
            Return If(x - borderWidth * 0.5 < mouseCoord.x And x + borderWidth * 0.5 + w > mouseCoord.x And y + borderWidth * 0.5 + h > mouseCoord.y And y - borderWidth * 0.5 < mouseCoord.y, 0, -1)
        End Function

        '// Set colours and position
        Public Sub SetBackgroundGradient(top As Colour, bottom As Colour)
            backgroundColour(0) = top
            backgroundColour(1) = top
            backgroundColour(2) = bottom
            backgroundColour(3) = bottom
        End Sub
        Public Sub SetBorderColour(col As Colour)
            For i = 0 To borderColour.Length - 1
                borderColour(i) = col
            Next
        End Sub
        Public Sub SetBackgroundColour(col As Colour)
            For i = 0 To backgroundColour.Length - 1
                backgroundColour(i) = col
            Next
        End Sub
        Public Overridable Sub SetCoords(inX As Single, inY As Single, inW As Single, inH As Single)
            x = inX
            y = inY
            w = inW
            h = inH
        End Sub

        '// Convert text to array of characters
        '// Needed to handle special characters
        Private Function TextToCharacters(text As String) As String()
            Dim characters As New List(Of String)
            Dim currentCharacter As New Text.StringBuilder()
            Dim specialCharacter As Boolean = False

            For i = 0 To text.Length - 1
                If text(i) = "."c Then
                    characters.Add("Dot")
                Else
                    If specialCharacter Then
                        If text(i) = ">" Then
                            characters.Add(currentCharacter.ToString())
                            specialCharacter = False
                        Else
                            currentCharacter.Append(text(i))
                        End If
                    Else
                        If text(i) = "<" Then
                            specialCharacter = True
                            currentCharacter.Clear()
                        Else
                            characters.Add(text(i))
                        End If
                    End If
                End If
            Next
            '// Each element is either a single character or a string describing the character
            '// e.g. "AltCharacter" is a special character, but the alt character is not a valid filename for the font image
            '// Therefore, the alternative name in <> must be used
            Return characters.ToArray()
        End Function

        '// Populate an array with the texture locations from the texture atlas
        Public Overridable Function AddTextCoordsToArray(ByRef position As Single(), ByRef texture As Single(), ByRef colour As Single(), index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            Dim cursorPosition As New CoordDataTypes.COORD2Sng(x, y + h - fontSize)
            Dim coordIndex As Integer = index
            Dim characters As String()
            Dim currentColour As Colour = fontColour

            '// Do not display text if the control is below the image layer and images have already been rendered
            If Not topZPass Or topZ Then
                '// Prevent no reference error
                If IsNothing(text) Then text = ""

                '// Generate array of characters
                characters = TextToCharacters(text)

                For i = 0 To characters.Length - 1
                    If characters(i)(0) = "#"c Then
                        '// Special character to change the font colour
                        '// E.g. <#FF0000> will set the text red
                        currentColour = New Colour(characters(i).Substring(1))
                    ElseIf characters(i) = "Newline" Then
                        '// <Newline> is a special character to move the position of the text
                        InsertNewline(cursorPosition)
                    Else
                        '// Find the coordinates of the character and add to array
                        AddTextCoordsToArray(position, texture, colour, characters(i), cursorPosition, textureMap, coordIndex + i * 6, currentColour)
                    End If
                Next
                Return index + text.Length * 6
            Else
                Return index
            End If
        End Function

        '// Set the position to the left and down one space
        Public Sub InsertNewline(ByRef cursorPosition As CoordDataTypes.COORD2Sng)
            cursorPosition.x = x
            cursorPosition.y -= fontSize
        End Sub

        '// Add data to buffers for a background rectangle and border
        Public Overridable Function AddBorderCoordsToArray(ByRef position As Single(), ByRef texture As Single(), ByRef colour As Single(), index As Integer, topZPass As Boolean) As Integer
            Dim dx As Single() = {0, 1, 1, 0, 1, 0}
            Dim dy As Single() = {0, 0, 1, 0, 1, 1}
            Dim colourLookup As Integer() = {2, 3, 1, 2, 1, 0}
            Dim averageColour As Single

            If Not topZPass Or topZ Then
                '// Display the border which is a larger rectangle
                For i = 0 To 5
                    position(3 * (i + index)) = x + dx(i) * w + (dx(i) - 0.5F) * borderWidth
                    position(3 * (i + index) + 1) = y + dy(i) * h + (dy(i) - 0.5F) * borderWidth
                    position(3 * (i + index) + 2) = 0.1
                    texture(2 * (i + index)) = 0
                    texture(2 * (i + index) + 1) = 0
                    colour(3 * (i + index)) = borderColour(colourLookup(i)).r
                    colour(3 * (i + index) + 1) = borderColour(colourLookup(i)).g
                    colour(3 * (i + index) + 2) = borderColour(colourLookup(i)).b
                Next
                '// Display the background which is a smaller rectangle
                For i = 0 To 5
                    position(3 * (i + index) + 18) = x + dx(i) * w
                    position(3 * (i + index) + 19) = y + dy(i) * h
                    position(3 * (i + index) + 20) = 0
                    texture(2 * (i + index) + 12) = 0
                    texture(2 * (i + index) + 13) = 0
                    colour(3 * (i + index) + 18) = backgroundColour(colourLookup(i)).r
                    colour(3 * (i + index) + 19) = backgroundColour(colourLookup(i)).g
                    colour(3 * (i + index) + 20) = backgroundColour(colourLookup(i)).b
                Next
                If Not enabled Then
                    '// Take an average of the colours, decreasing the saturation
                    For i = 0 To 10
                        averageColour = 0
                        For j = 0 To 2
                            averageColour += colour(3 * (i + index) + j)
                        Next
                        averageColour /= 3
                        For j = 0 To 2
                            colour(3 * (i + index) + j) = averageColour
                        Next
                    Next
                End If
                Return index + 12
            Else
                Return index
            End If
        End Function

        '// Add a character to the buffer to be drawn
        Protected Sub AddTextCoordsToArray(ByRef position As Single(), ByRef texture As Single(), ByRef colourArr As Single(), letter As String, ByRef cursorPosition As CoordDataTypes.COORD2Sng, textureMap As TextureLoader.TextureMap, coordIndex As Integer, currentColour As Colour)
            Dim dx As Single() = {0, 1, 1, 0, 1, 0}
            Dim dy As Single() = {0, 0, 1, 0, 1, 1}
            Dim colourLookup As Integer() = {2, 3, 1, 2, 1, 0}
            Dim averageColour As Single
            Dim letterDimensions As TextureLoader.TextureMap.ImageLocationSng

            '// Get the location of the character in the texture map atlas
            letterDimensions = textureMap.GetSubImage(letter)

            For i = 0 To 5
                '// Characters are drawn as a rectangle with a texture applied
                '// Width of rectangl depends on width of character (i is thinner than w)
                position((coordIndex + i) * 3) = cursorPosition.x + dx(i) * fontSize * letterDimensions.width / letterDimensions.height
                position((coordIndex + i) * 3 + 1) = cursorPosition.y + dy(i) * fontSize
                position((coordIndex + i) * 3 + 2) = 0
                texture((coordIndex + i) * 2) = letterDimensions.left + letterDimensions.width * dx(i)
                texture((coordIndex + i) * 2 + 1) = letterDimensions.top + letterDimensions.height * dy(i)
                If Not enabled Then
                    '// Greyscale the character if not enabled
                    averageColour = (currentColour.r + currentColour.g + currentColour.b) / 3
                    fontColour.r = averageColour
                    fontColour.g = averageColour
                    fontColour.b = averageColour
                End If
                '// Add colours to array
                colourArr((coordIndex + i) * 3) = currentColour.r
                colourArr((coordIndex + i) * 3 + 1) = currentColour.g
                colourArr((coordIndex + i) * 3 + 2) = currentColour.b
            Next

            '// Draw next character with offset equal to width of previous one
            cursorPosition.x += fontSize * letterDimensions.width / letterDimensions.height
            coordIndex += 6
        End Sub
    End Class

    '// GUI control with click functionality
    Public Class Button
        Inherits Label

        '// Delegate subroutine to be called when clicked on
        Public Delegate Sub EventClickDel(ByRef frmArray As Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        Public EventClick As EventClickDel
        Public eventArgs As Integer

        '// Invoke the click subroutine
        Public Overrides Sub OnClick(collision As Integer)
            MyBase.OnClick(collision)
            EventClick.Invoke(GameWorld.frmArray, GameWorld.activeForm, eventArgs, collision)
        End Sub

        '// Buttons have a different default hover animation
        Public Overrides Sub OnHover(collision As Integer)
            MyBase.OnHover(collision)
            If transparentBackground Then
                '// Make text larger
                fontSize += 0.01F
                y -= 0.005F
                x -= 0.005F
            Else
                '// Make colours brighter
                For i = 0 To backgroundColour.Length - 1
                    backgroundColour(i).r *= 1.2F
                    backgroundColour(i).g *= 1.2F
                    backgroundColour(i).b *= 1.2F
                Next
            End If
        End Sub

        '// Restore original styles
        Public Overrides Sub OnDehover()
            If hover = 0 Then
                If transparentBackground Then
                    '// Make text smaller
                    fontSize -= 0.01F
                    y += 0.005F
                    x += 0.005F
                Else
                    '// Make background darker
                    For i = 0 To backgroundColour.Length - 1
                        backgroundColour(i).r /= 1.2F
                        backgroundColour(i).g /= 1.2F
                        backgroundColour(i).b /= 1.2F
                    Next
                End If
            End If
            MyBase.OnDehover()
        End Sub

    End Class

    '// GUI control for a drop down list
    Public Class DropDown
        Inherits Button
        Public ListItems As New List(Of String)
        Public ListItemBase As New Button
        Public ShowList As Boolean
        Private currentSelected As Integer
        Public Overrides Property text As String
            Get
                If currentSelected < ListItems.Count Then
                    Return ListItems(currentSelected)
                Else
                    Return ""
                End If
            End Get
            Set(value As String)
                MyBase.text = value
            End Set
        End Property
        Sub New()
            ListItemBase.SetCoords(x, y - 0.05F, w, 0.05)
            ShowList = False
        End Sub

        '// Ensure all children are displayed if necessary
        Public Overrides Function AddBorderCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, topZPass As Boolean) As Integer
            index = MyBase.AddBorderCoordsToArray(position, texture, colour, index, topZPass)
            '// If the top button has been clicked on, show the list of options
            If ShowList Then
                For i = 0 To ListItems.Count - 1
                    If hover - 1 = i Then ListItemBase.OnHover(0)
                    ListItemBase.borderWidth = borderWidth
                    ListItemBase.text = ListItems(i)
                    ListItemBase.y = y - (i + 1) * (ListItemBase.h + ListItemBase.borderWidth)
                    index = ListItemBase.AddBorderCoordsToArray(position, texture, colour, index, topZPass)
                    If hover - 1 = i Then ListItemBase.OnDehover()
                Next
            End If
            Return index
        End Function

        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            index = MyBase.AddTextCoordsToArray(position, texture, colour, index, text, textureMap, topZPass)
            'If the top button has been clicked on, show the drop down options
            If ShowList Then
                For i = 0 To ListItems.Count - 1
                    If hover - 1 = i Then ListItemBase.OnHover(0)
                    ListItemBase.borderWidth = borderWidth
                    ListItemBase.text = ListItems(i)
                    ListItemBase.y = y - (i + 1) * (ListItemBase.h + ListItemBase.borderWidth)
                    index = ListItemBase.AddTextCoordsToArray(position, texture, colour, index, ListItemBase.text, textureMap, topZPass)
                    If hover - 1 = i Then ListItemBase.OnDehover()
                Next
            End If
            Return index
        End Function

        '// Ensure styles are consistent throughout
        Public Sub CopyStylesToList()
            ListItemBase.SetBackgroundColour(backgroundColour(0))
            ListItemBase.SetBorderColour(borderColour(0))
            ListItemBase.borderWidth = borderWidth
            ListItemBase.fontSize = fontSize
            ListItemBase.SetCoords(x, y, w, h)
        End Sub

        Public Overrides Sub OnClick(collision As Integer)
            selected = True
            If collision = 0 Then
                '// Set flag to show drop down list
                ShowList = True
            Else
                '// If drop down list is visible, select element
                currentSelected = collision - 1
                ListItemBase.OnClick(collision - 1)
                '// Hide list
                ShowList = False
            End If
        End Sub
        Public Overrides Sub OnHover(collision As Integer)
            If collision = 0 Then MyBase.OnHover(collision)
            hover = collision
        End Sub
        Public Overrides Sub OnDehover()
            If hover = 0 Then MyBase.OnDehover()
            hover = -1
        End Sub
        Public Overrides Sub OnDeclick(collision As Integer)
            MyBase.OnDeclick(collision)
            ShowList = False
        End Sub

        '// Check for mouse collision with individual elements
        Public Overrides Function MouseCollision(mouseCoord As Form.COORD) As Integer
            If MyBase.MouseCollision(mouseCoord) > -1 Then Return 0
            '// If drop down is visible, check children for mouse hover
            If ShowList Then
                For i = 0 To ListItems.Count - 1
                    ListItemBase.y = y - (i + 1) * (ListItemBase.h + ListItemBase.borderWidth)
                    If ListItemBase.MouseCollision(mouseCoord) > -1 Then Return i + 1
                Next
            End If
            Return -1
        End Function
    End Class

    '// Provides a control that the user can type text in
    Public Class TextBox
        Inherits Label
        Public placeholder As String

        '// Different selection transitions to show it is a text box
        Public Overrides Sub OnClick(collision As Integer)
            If Not selected Then
                borderWidth *= 2
                backgroundColour(2) *= 0.5
                backgroundColour(3) *= 0.5
            End If
            MyBase.OnClick(collision)
        End Sub
        Public Overrides Sub OnDeclick(collision As Integer)
            If selected Then
                borderWidth *= 0.5F
                backgroundColour(2) *= 2
                backgroundColour(3) *= 2
            End If
            MyBase.OnDeclick(collision)
        End Sub

        '// Add key presses to the text
        Public Overrides Sub KeyPress(keys() As Integer)
            MyBase.KeyPress(keys)
            For i = 0 To keys.Length - 1
                Select Case keys(i)
                    Case 8
                        '// Case for backspace
                        If text.Length > 0 Then
                            text = text.Substring(0, text.Length - 1)
                        End If
                    Case Else
                        '// Check for alphanumeric character with whitespace
                        '// Do not allow special characters as this could create invalid file names
                        If System.Text.RegularExpressions.Regex.Match(Chr(keys(i)), "[A-Za-z0-9 ]").Success Then
                            text &= Chr(keys(i))
                        End If
                End Select
            Next
        End Sub
        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            Dim tempColour As Colour
            Dim returnValue As Integer
            If text = "" And Not selected Then
                '// Add a placeholder text if not clicked on and empty
                tempColour = fontColour
                fontColour = New Colour("555555")
                returnValue = MyBase.AddTextCoordsToArray(position, texture, colour, index, placeholder, textureMap, topZPass)
                fontColour = tempColour
                Return returnValue
            Else
                '// Add standard text
                Return MyBase.AddTextCoordsToArray(position, texture, colour, index, text, textureMap, topZPass)
            End If
        End Function
    End Class

    '// Hides the text that the user is typing
    Public Class TextBoxPassword
        Inherits TextBox

        Public Overrides Function AddTextCoordsToArray(ByRef position() As Single, ByRef texture() As Single, ByRef colour() As Single, index As Integer, text As String, textureMap As TextureLoader.TextureMap, topZPass As Boolean) As Integer
            '// Replace current text with a list of XXX of the same length
            Return MyBase.AddTextCoordsToArray(position, texture, colour, index, "".PadRight(text.Length, "X"c), textureMap, topZPass)
        End Function
    End Class

    '// Stores style information to be copied to many controls
    Public Class StyleSheet
        Public x As Single
        Public y As Single
        Public w As Single
        Public h As Single
        Public text As String
        Public fontSize As Single
        Public borderWidth As Single
        Public borderColour(3) As Colour
        Public backgroundColour(3) As Colour
        Public fontColour As Colour
        Public transparentBackground As Boolean

        Public Sub SetBackgroundGradient(top As Colour, bottom As Colour)
            backgroundColour(0) = top
            backgroundColour(1) = top
            backgroundColour(2) = bottom
            backgroundColour(3) = bottom
        End Sub
        Public Sub SetBorderColour(col As Colour)
            For i = 0 To borderColour.Length - 1
                borderColour(i) = col
            Next
        End Sub
        Public Sub SetBackgroundColour(col As Colour)
            For i = 0 To backgroundColour.Length - 1
                backgroundColour(i) = col
            Next
        End Sub
        Public Sub SetCoords(inX As Single, inY As Single, inW As Single, inH As Single)
            x = inX
            y = inY
            w = inW
            h = inH
        End Sub
    End Class

    Public Enum Controls
        Label = 0
        RenderTarget = 1
        GridView = 2
    End Enum
End Class

