Option Strict On
Imports NEA.CoordDataTypes
Imports NEA.OpenGLImporter
'// Class to represent an entity in the game
Public Class Mob
    '// Each transition between animations lasts for 0.2 seconds
    Public Const ANIMATION_FADE_OUT_TIME As Double = 0.2

    '// Variable declarations
    Public maxHealth As Integer
    Private currentHealth As Integer
    Public lastHurt As Single
    Public Property health As Integer
        Get
            Return currentHealth
        End Get
        Set(value As Integer)
            If value < currentHealth Then
                lastHurt = CSng(Timer)
            End If
            currentHealth = value
        End Set
    End Property
    Public model As GLTFModel
    Public animationProgress As Single
    Public animationList As New List(Of AnimationQueueItem)
    Private currentAnimation As String
    Private previousAnimation As String
    Public currentAnimationStartTime As Double
    Public actionInProgress As Boolean
    Public children As New List(Of Mob)
    Public childrenBones As New List(Of Integer)
    Public identifier As Integer
    Public wasKilled As Boolean
    Public skin As Integer = 0
    Public size As New COORD3Sng(1, 1, 1)
    Public floorHeight As Single
    Public transformationMatricesInverse As Matrices() = {New Matrices(4, 4, True)}
    Public transformationMatricesOrigin As Matrices() = {New Matrices(4, 4, True)}
    Public soundAttack As Integer
    Protected soundGrowl As Integer
    Protected audio As AudioManager
    Protected highlightCol As COORD3Sng
    Public closest As Boolean
    Public Property animationName As String
        Get
            Return currentAnimation
        End Get
        Set(value As String)
            If currentAnimation <> value And Not actionInProgress Then
                currentAnimationStartTime = Timer
                previousAnimation = currentAnimation
                currentAnimation = value
                If previousAnimation <> "" And previousAnimation <> "punch" And previousAnimation <> "dodge" Then
                    animationList.Add(New AnimationQueueItem(previousAnimation, Timer))
                End If
            End If
        End Set
    End Property
    Public location As COORD3Sng
    Public locationOffset As COORD3Sng
    Public targetRotation As Single
    Public actualRotation As Single
    Public Property rotation As Single
        Get
            Return actualRotation
        End Get
        Set(value As Single)
            targetRotation = value
        End Set
    End Property
    Public elevation As Single
    Public healthVisible As Boolean
    Public targeted As Boolean
    Public rootNodeTranslatePrev As New COORD3Sng(0, 0, 0)
    Public rootNodeTranslate As New COORD3Sng(0, 0, 0)
    Public bypassAnimationTranslation As Boolean
    Public canBeTargeted As Boolean

    '// Get angle between line between mob and player from the forward direction of the player
    Public Function GetAngle(characterLocation As COORD3Sng, characterRotation As Single) As Single
        Dim characterForwardPoint As New COORD3Sng(CSng(characterLocation.x + Math.Sin(characterRotation)), characterLocation.y, CSng(characterLocation.z + Math.Cos(characterRotation)))
        Return Math.Abs(Theta(characterLocation, location, characterForwardPoint))
    End Function

    '// Check if the mob is within an angle from directly in front of the player
    Public Function InRangeAngle(cutoff As Single, characterLocation As COORD3Sng, characterRotation As Single) As Boolean
        Dim angle As Single = GetAngle(characterLocation, characterRotation)
        Return angle < cutoff And canBeTargeted
    End Function

    '// Check if the mob is in range of an attack
    Public Function InRange(characterLocation As CoordDataTypes.COORD3Sng, characterRotation As Single, maxDistance As Single) As Boolean
        Dim isInRange As Boolean = False
        Dim dist As Single
        '// Check if the mob is in front of the player
        If InRangeAngle(0.7, characterLocation, characterRotation) Then
            '// Check if the mob is sufficiently close to the player
            dist = Distance(location, characterLocation)
            If dist < maxDistance Then
                isInRange = True
            End If
        End If
        Return isInRange And canBeTargeted
    End Function

    '// Apply translation animations to the mob
    Public Sub UpdateAnimationPosition(fullPosition As Boolean)
        Dim distanceToMove As New COORD3Sng
        Dim rotSin As Single = CSng(Math.Sin(rotation))
        Dim rotCos As Single = CSng(Math.Cos(rotation))
        '// Get difference in positions between frames
        distanceToMove.x = rootNodeTranslate.x - rootNodeTranslatePrev.x
        distanceToMove.z = rootNodeTranslate.z - rootNodeTranslatePrev.z
        '// Prevent mob teleporting back at the start of the walk sequence
        If distanceToMove.z > -0.5 And fullPosition Then
            location.x += rotCos * distanceToMove.x + rotSin * distanceToMove.z
            location.z += rotCos * distanceToMove.z - rotSin * distanceToMove.x
        End If
        rootNodeTranslatePrev.x = rootNodeTranslate.x
        rootNodeTranslatePrev.y = rootNodeTranslate.y
        rootNodeTranslatePrev.z = rootNodeTranslate.z
        locationOffset.y = rootNodeTranslate.y
    End Sub

    '// Rotate from one angle to another
    '// Ensure the shortest direction is taken
    Public Sub SmoothRotation(deltaT As Single)
        Dim rotationDifference As Single
        Dim wasClockwise As Boolean
        '// Get angle to rotate by
        rotationDifference = targetRotation - actualRotation
        rotationDifference = (rotationDifference + CSng(Math.PI * 2)) Mod CSng(Math.PI * 2)

        If rotationDifference > Math.PI Then
            '// Anticlockwise
            actualRotation += CSng(Math.PI * 20) - deltaT * 10
            wasClockwise = False
        Else
            '// Clockwise
            actualRotation += deltaT * 10
            wasClockwise = True
        End If
        actualRotation = actualRotation Mod CSng(Math.PI * 2)

        rotationDifference = targetRotation - actualRotation
        rotationDifference = (rotationDifference + CSng(Math.PI * 2)) Mod CSng(Math.PI * 2)

        '// If close to target, snap to target
        '// Prevents small amplitude oscillation about an angle
        If Math.Abs(rotationDifference) < 0.1 Or (rotationDifference <= Math.PI Xor wasClockwise) Then
            actualRotation = targetRotation
        End If
    End Sub

    '// Reduce health by damage amount
    Public Sub TakeDamage(damageAmount As Integer)
        health -= damageAmount
    End Sub

    '// Create a child and bind it to a bone
    Public Sub AddChild(inItem As Mob, inTargetBone As String)
        children.Add(inItem)
        childrenBones.Add(model.GetIndexOfBone(inTargetBone, model.nodes))
    End Sub

    '// Give the player a reward on death
    Public Sub GiveLoot(character As Player)
        For i = 0 To 5
            '// Random quantity of loot
            If Rnd() > 0.2 Then
                character.inventoryItems.AddItem("Loot", 1, 1)
            End If
        Next
    End Sub

    '// Initialise mob
    Sub New(ByRef inModel As GLTFModel, ByRef inAudio As AudioManager)
        model = inModel
        '// Default value for health
        health = 100
        maxHealth = 100
        '// Bind references to audio player and load sounds
        audio = inAudio
        soundGrowl = audio.LoadResource("Grr.wav")
        soundAttack = audio.LoadResource("Attack.wav")
        canBeTargeted = True
    End Sub

    '// Remove animations if they finished 0.2 seconds ago
    Public Sub CleanUpAnimations()
        While animationList.Count > 0 AndAlso Timer - animationList(0).endTime > ANIMATION_FADE_OUT_TIME
            '// Animation list is a queue, so the first item can be removed as it ended first
            animationList.RemoveAt(0)
        End While
    End Sub

    '// Get model matrix, with additional transform if the model is bound to a parent mob
    Public Function GetModelMatrixBoth(isItem As Boolean, parentMatrices As Matrices(), targetBone As Integer) As Matrices
        If isItem Then
            Return GetModelItemMatrices(GetModelMatrix(), parentMatrices, targetBone)
        Else
            Return GetModelMatrix()
        End If
    End Function

    '// Render the model to the display
    Public Sub Display(program As UInt32, matrixRelative As Matrices, matrixView As Matrices, matrixPerspective As Matrices, isItem As Boolean, parentMatrices As Matrices(), targetBone As Integer)
        '// Check if the mob hsa finished the death state
        If Not wasKilled Then
            '// Calculate matrices
            Dim matrixNormal As New Matrices(3, 3, True)
            Dim matrixModel As Matrices
            Dim currentBuffer As Int32
            Dim childrenMatrices As Matrices()
            matrixModel = GetModelMatrixBoth(isItem, parentMatrices, targetBone)
            childrenMatrices = GetItemMatrices(matrixModel)
            '// Bind model vertex buffers
            model.context.glBindVertexArray(model.VAO)
            '// Debug bindings
            glGetVertexAttribiv(0, GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING, currentBuffer)
            glGetVertexAttribiv(1, GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING, currentBuffer)
            glGetVertexAttribiv(2, GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING, currentBuffer)
            '// Load textures
            TextureLoader.BindTextures(model.textures)
            model.context.glBindTexture(GL_TEXTURE_0, model.texturesAlbedo(skin))
            '// Check if the model should be highlighted
            highlightCol = GetHighlightColour()
            '// Load uniform variables
            glUniform3f(glGetUniformLocationStr(CInt(program), "highlightCol"), highlightCol.x, highlightCol.y, highlightCol.z)
            model.GLTFMatrices(animationProgress, CInt(program), matrixRelative, matrixView, matrixPerspective, matrixModel, matrixNormal, animationName, CSng(currentAnimationStartTime), animationList.ToArray(), transformationMatricesInverse)
            '// Draw indexed vertices
            glDrawElements(GL_TRIANGLES, CInt(model.numVertices), model.indexType, 0)
            '// Display all children
            For i = 0 To children.Count - 1
                children(i).Display(program, matrixRelative, matrixView, matrixPerspective, True, childrenMatrices, childrenBones(i))
            Next
        End If
    End Sub

    '// By default, no highlight is applied
    Protected Overridable Function GetHighlightColour() As COORD3Sng
        Return New COORD3Sng(0, 0, 0)
    End Function

    '// Refresh model matrix cache
    Public Sub UpdateMatrices()
        rootNodeTranslate = model.GetTransformationMatrices(animationProgress, animationName, CSng(currentAnimationStartTime), animationList.ToArray(), transformationMatricesOrigin, transformationMatricesInverse)
        For i = 0 To children.Count - 1
            children(i).UpdateMatrices()
        Next
    End Sub

    '// Generate matrix for the model
    Public Function GetModelMatrix() As Matrices
        Dim matrixModel As New Matrices(4, 4, True)
        matrixModel = Matrices.Scale({size.x, size.y, size.z})
        '// Apply rotation
        matrixModel = Matrices.RotatePitchUp(-elevation, matrixModel)
        matrixModel = Matrices.RotateXZPlaneClockwise(-rotation, matrixModel)
        '// Apply mob translation
        matrixModel = Matrices.Translate(location, matrixModel)
        '// Apply animation translation
        matrixModel = Matrices.Translate(locationOffset, matrixModel)
        Return matrixModel
    End Function

    '// Generate matrix for a child mob by applying parent matrix first
    Public Function GetModelItemMatrices(matrixModel As Matrices, boneTransformMatrices As Matrices(), targetBone As Integer) As Matrices
        Return Matrices.Multiply(matrixModel, boneTransformMatrices(targetBone))
    End Function

    Public Function GetItemMatrices(parentTransform As Matrices) As Matrices()
        Dim itemMatrices As Matrices() = Matrices.Copy(transformationMatricesOrigin)
        Dim rescale As Single

        For i = 0 To itemMatrices.Length - 1
            rescale = 1 / itemMatrices(i).ScaleApplied
            '// Some models are scaled at the root, and this needs to be removed
            For j = 0 To itemMatrices(i).data.Length - 1
                If j Mod 4 <> 3 Then
                    '// Preserve the translations
                    itemMatrices(i).data(j) *= rescale
                End If
            Next
        Next

        For i = 0 To itemMatrices.Length - 1
            itemMatrices(i) = Matrices.Multiply(parentTransform, itemMatrices(i))
        Next

        Return itemMatrices
    End Function

    '// Structure to fade out an animation
    Public Structure AnimationQueueItem
        Public name As String
        Public endTime As Double
        Sub New(inName As String, inEndTime As Double)
            name = inName
            endTime = inEndTime
        End Sub
    End Structure

End Class

