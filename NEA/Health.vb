Option Strict On
Imports NEA.CoordDataTypes
Imports System.Runtime.InteropServices
Imports NEA.OpenGLImporter

'// Manager class for health and stamina bars and values
Public Class Health
    '// References to form control objects
    Public healthNumeric As GUIObject.Label
    Public healthBar As GUIObject.Bar
    Public staminaNumeric As GUIObject.Label
    Public staminaBar As GUIObject.Bar
    Public enemyHealth As GUIObject.HealthBarCollection

    '// Initialisation
    Sub New(healthStyles As GUIObject.StyleSheet)
        '// Create new form controls to display health
        healthNumeric = New GUIObject.Label()
        staminaNumeric = New GUIObject.Label()
        healthBar = New GUIObject.Bar(New GUIObject.Colour("dd2020"))
        staminaBar = New GUIObject.Bar(New GUIObject.Colour("2020dd"))
        enemyHealth = New GUIObject.HealthBarCollection(New GUIObject.Colour("dd2020"))

        '// Set position and styles of health bars
        healthNumeric.SetStyles(healthStyles)
        staminaNumeric.SetStyles(healthStyles)
        healthNumeric.fontColour = New GUIObject.Colour("ff0000")
        staminaNumeric.fontColour = New GUIObject.Colour("0000ff")
        healthNumeric.SetCoords(-0.9, 0.85, 0.3, 0.07)
        staminaNumeric.SetCoords(-0.9, 0.75, 0.3, 0.07)
        healthNumeric.fontSize = 0.05F
        staminaNumeric.fontSize = 0.05F

        healthBar.SetCoords(-0.9, 0.85, 0.4, 0.07)
        staminaBar.SetCoords(-0.9, 0.75, 0.4, 0.07)

        '// Initialise health bar data array
        enemyHealth.healthPositions = New List(Of COORD3Sng)
    End Sub

    '// Prepare health bars to be displayed
    Public Sub RenderHealth(character As Player, graphicsSettings As GameWorld.GraphicsSettings, transformMatrix As Matrices, enemies As EnemyManager)
        '// Update player health and stamina bars
        staminaNumeric.text = Math.Max(Math.Floor(character.stamina), 0).ToString()
        healthNumeric.text = Math.Max(Math.Floor(character.health), 0).ToString()
        healthBar.barMaxValue = CInt(character.maxHealth)
        healthBar.currentValue = CInt(character.health)
        staminaBar.barMaxValue = CInt(character.maxStamina)
        staminaBar.currentValue = CInt(character.stamina)

        '// Check for current health bar settings
        If graphicsSettings.GetGraphicsSettings(GameWorld.GraphicsSettings.SettingsIndex.HealthBar) = 1 Then
            '// Display health bar and offset numeric health
            healthNumeric.x = healthBar.w + healthBar.x + 0.02F
            staminaNumeric.x = staminaBar.w + staminaBar.x + 0.02F
            healthBar.visible = True
            staminaBar.visible = True
        Else
            '// Hide health bar and move numeric health back
            healthNumeric.x = healthBar.x
            staminaNumeric.x = staminaNumeric.x
            healthBar.visible = False
            staminaBar.visible = False
        End If

        '// Show or hide numeric health
        If graphicsSettings.GetGraphicsSettings(GameWorld.GraphicsSettings.SettingsIndex.NumericHealth) = 1 Then
            healthNumeric.visible = True
            staminaNumeric.visible = True
        Else
            healthNumeric.visible = False
            staminaNumeric.visible = False
        End If

        '// Load enemy health bars
        RenderEnemyHealthBars(character, enemies, transformMatrix)
    End Sub

    '// Loads enemy health bars
    Private Sub RenderEnemyHealthBars(character As Player, enemies As EnemyManager, transformMatrix As Matrices)
        Dim location As COORD3Sng

        enemyHealth.worldSpaceTransform = transformMatrix
        '// Clear all previous health bars
        enemyHealth.healthPositions.Clear()
        enemyHealth.healthValues.Clear()
        enemyHealth.healthMaxValues.Clear()

        '// Add enemy health bars
        For i = 0 To enemies.enemies.Count - 1
            If enemies.enemies(i).displayHealthBar Then
                '// Get location  of health bar, 2m above the base of the enemy
                location = enemies.enemies(i).location
                location.y += 2 * enemies.enemies(i).size.y
                '// Add specific data to array in the control
                enemyHealth.healthPositions.Add(location)
                enemyHealth.healthMaxValues.Add(CInt(enemies.enemies(i).maxHealth))
                enemyHealth.healthValues.Add(CInt(enemies.enemies(i).health))
            End If
        Next
    End Sub

End Class

