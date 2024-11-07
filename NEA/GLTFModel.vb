Option Strict On
Imports System.Console
Imports System.Runtime.InteropServices
Imports NEA.OpenGLImporter
Imports NEA.CoordDataTypes
'// Class to load and store an animated model
Public Class GLTFModel
    Const MESH_CONST As Integer = 0
    Const MODEL_DIRECTORY As String = "Resources\Models\"

    Public modelName As String

    '// Variable declaration
    Private buffers As Buffer()
    Private bufferViews As BufferView()
    Private accessors As Accessor()
    Private meshes As Mesh()
    Public nodes As Node()
    Private channels As Channel()
    Private samplers As Sampler()
    Private buffersPtr As IntPtr
    Public numVertices As UInt32
    Public jointsMapping As Int32()
    Public indexType As UInt32
    Public textures As UInt32()
    Public texturesAlbedo As UInt32()
    Public VAO As UInt32
    Public VAOShadow As UInt32
    Public animations As AnimationFrames
    Private rootNode As Integer
    Public context As OpenGLContext
    Private bufferPosition, bufferWeights, bufferNormals, bufferJoints, bufferTexture, bufferTangents As UInt32

    '// Functions to get accessors containing data as specified in the function name
    Private Function GetRotationMatrix(interpolatedRotation As Quaternion.Quaternion) As Matrices
        Return Quaternion.QuarternionToMatrix(interpolatedRotation)
    End Function

    Public Function GetWeights(meshID As Integer) As Accessor
        If meshes(meshID).combinedPrimitives.weights > 0 Then
            Return accessors(meshes(meshID).combinedPrimitives.weights)
        Else
            Return New Accessor()
        End If
    End Function

    Public Function GetIndices(meshID As Integer) As Accessor
        Return accessors(meshes(meshID).combinedPrimitives.indices)
    End Function

    Public Function GetPosition(meshID As Integer) As Accessor
        Return accessors(meshes(meshID).combinedPrimitives.position)
    End Function

    Public Function GetNormal(meshID As Integer) As Accessor
        Return accessors(meshes(meshID).combinedPrimitives.normal)
    End Function

    Public Function GetJoints(meshID As Integer) As Accessor
        If meshes(meshID).combinedPrimitives.joints > 0 Then
            Return accessors(meshes(meshID).combinedPrimitives.joints)
        Else
            Return New Accessor()
        End If
    End Function

    Public Function GetTextureCoords(meshID As Integer) As Accessor
        If meshes(meshID).combinedPrimitives.texture > 0 Then
            Return accessors(meshes(meshID).combinedPrimitives.texture)
        Else
            Return New Accessor()
        End If
    End Function

    Private Function IsIndexed(meshID As Integer) As Boolean
        Return meshes(meshID).combinedPrimitives.indexed
    End Function

    Public Function GetSamplerInput(samplerID As Integer) As Accessor
        Return accessors(samplers(samplerID).input)
    End Function

    Public Function GetSamplerOutput(samplerID As Integer) As Accessor
        Return accessors(samplers(samplerID).output)
    End Function

    '// Linear search for bone by index
    Public Function GetIndexOfBone(boneID As String, nodes As Node()) As Integer
        For i = 0 To nodes.Length - 1
            If nodes(i).name = boneID Or nodes(i).name.Substring(1, nodes(i).name.Length - 2) = boneID Then
                Return i
            End If
        Next
        Return -1
    End Function

    '// Used for debugging to check matrices scale properly
    Public Shared Sub SizeCheck(m As Matrices())
        Dim s As Double
        For i = 0 To m.Length - 1
            For j = 0 To 2
                s = 0
                For k = 0 To 2
                    s += m(i).data(j * 4 + k) ^ 2
                Next
                WriteLine(Math.Sqrt(s))
            Next
        Next
        ReadLine()
    End Sub

    '// Get matrices representing combined effectof animation transformations
    Public Function GetTransformationMatrices(time As Single, animationName As String, animationStartTime As Single, fadeOut As Mob.AnimationQueueItem(), ByRef matricesOrigin As Matrices(), ByRef matricesInverse As Matrices()) As COORD3Sng
        Dim rootNodeTranslate As New COORD3Sng(0, 0, 0)
        Dim animationNames As String() = GetAnimationNames(fadeOut, animationName)
        Dim animationWeights As Single() = GetAnimationWeights(fadeOut, animationStartTime)
        Dim rotationMatrices As Matrices() = GetRotationMatrices(time, animationNames, animationWeights)
        Dim translationMatrices As Matrices() = GetTranslationMatrices(time, animationNames, animationWeights, rootNodeTranslate)
        Dim scaleMatrices As Matrices() = GetScaleMatrices(time, animationNames, animationWeights)
        Dim recursionApplied(nodes.Length - 1) As Boolean
        Dim matrixArr(rotationMatrices.Length - 1) As Matrices
        Dim matrixArrInverse(rotationMatrices.Length - 1) As Matrices

        '// Get combined matrix for each bone
        For i = 0 To matrixArr.Length - 1
            matrixArr(i) = Matrices.Multiply(translationMatrices(i), Matrices.Multiply(rotationMatrices(i), scaleMatrices(i)))
            If nodes(i).hasMatrix Then
                matrixArr(i).data = nodes(i).matrix
            End If
        Next

        '// Ensure that parent matrices affect children matrices as per GLTF specification
        For i = 0 To matrixArr.Length - 1
            ApplyRecursiveMatrix(matrixArr, GetParents(nodes), recursionApplied, i)
        Next

        '// Apply inverse matrices
        For i = 0 To matrixArrInverse.Length - 1
            matrixArrInverse(i) = matrixArr(i)
        Next

        For i = 0 To matrixArr.Length - 1
            matrixArrInverse(i) = Matrices.Multiply(matrixArrInverse(i), nodes(i).inverseBindMatrix)
        Next

        '// Create new root bone if it is missing
        If rootNode <> -1 Then
            Dim matrixTranslationRoot As New Matrices(4, 4, True)
            Dim parent As Integer
            matrixTranslationRoot = Matrices.Translate(rootNodeTranslate, matrixTranslationRoot)
            parent = nodes(rootNode).parent
            matrixTranslationRoot = Matrices.Multiply(matrixArr(parent), matrixTranslationRoot)
            rootNodeTranslate.x = matrixTranslationRoot.data(3)
            rootNodeTranslate.y = matrixTranslationRoot.data(7)
            rootNodeTranslate.z = matrixTranslationRoot.data(11)
        End If

        matricesOrigin = matrixArr
        matricesInverse = matrixArrInverse

        Return rootNodeTranslate
    End Function

    '// Used for blending multiple animations
    Private Function GetAnimationWeights(fadeOut As Mob.AnimationQueueItem(), animationStartTime As Single) As Single()
        Dim total As Single
        Dim animationWeights(fadeOut.Length) As Single

        '// Animation weight decreases as time since animation end increases
        For i = 0 To fadeOut.Length - 1
            animationWeights(i) = 1 - CSng((Timer - fadeOut(i).endTime) / Mob.ANIMATION_FADE_OUT_TIME)
        Next

        animationWeights(animationWeights.Length - 1) = CSng((Timer - animationStartTime) / Mob.ANIMATION_FADE_OUT_TIME)

        '// Get absolute value
        For i = 0 To animationWeights.Length - 1
            If animationWeights(i) < 0 Then animationWeights(i) *= -1
        Next

        '// Normalise weights
        For i = 0 To animationWeights.Length - 1
            total += animationWeights(i)
        Next
        For i = 0 To animationWeights.Length - 1
            animationWeights(i) /= total
        Next

        Return animationWeights
    End Function

    '// Concatenate previous animation names and the current animation name
    Private Function GetAnimationNames(fadeOut As Mob.AnimationQueueItem(), animationName As String) As String()
        Dim animationNames(fadeOut.Length) As String

        For i = 0 To fadeOut.Length - 1
            animationNames(i) = fadeOut(i).name
        Next

        If IsNothing(animationName) Then animationName = ""
        animationNames(animationNames.Length - 1) = animationName

        Return animationNames
    End Function

    '// Add rotation matrices to an array
    Private Function GetRotationMatrices(time As Single, animationNames As String(), animationWeights As Single()) As Matrices()
        Dim matrixLst As New List(Of Matrices)

        For i = 0 To nodes.Length - 1
            If nodes(i).rotationAnimation > -1 Then
                '// Get rotation matrix
                matrixLst.Add(GetRotationMatrix(samplers(nodes(i).rotationAnimation), time, animationNames, animationWeights))
            Else
                '// Use default rotation matrix from default quaternion
                matrixLst.Add(Quaternion.QuarternionToMatrix(nodes(i).rotation))
            End If
        Next
        Return matrixLst.ToArray()
    End Function

    '// Add translation matrices to an array
    Private Function GetTranslationMatrices(time As Single, animationNames As String(), animationWeights As Single(), ByRef rootNodeTranslate As COORD3Sng) As Matrices()
        Dim matrixLst As New List(Of Matrices)
        For i = 0 To nodes.Length - 1
            If nodes(i).translationAnimation > -1 Then
                If i = rootNode Then
                    rootNodeTranslate = GetTranslationCoord(samplers(nodes(i).translationAnimation), time, animationNames(animationNames.Length - 1))
                    matrixLst.Add(Matrices.Translate(nodes(i).translation))
                Else
                    matrixLst.Add(GetTranslationMatrix(samplers(nodes(i).translationAnimation), time, animationNames(animationNames.Length - 1)))
                End If
            Else
                '// If there is no animation, add the default translation
                matrixLst.Add(Matrices.Translate(nodes(i).translation))
            End If
        Next
        Return matrixLst.ToArray()
    End Function

    '// Animations do not change the scale matrices, so the default is used
    Private Function GetScaleMatrices(time As Single, animationNames As String(), animationWeights As Single()) As Matrices()
        Dim matrixLst As New List(Of Matrices)
        For i = 0 To nodes.Length - 1
            matrixLst.Add(Matrices.Scale(nodes(i).scale))
        Next
        Return matrixLst.ToArray()
    End Function

    '// Get quaternion from animation frame
    Private Function GetRotationQuaternionInterpolated(aniSampler As Sampler, time As Single, animationName As String) As Quaternion.Quaternion
        Dim prevQ As New Quaternion.Quaternion
        Dim nextQ As New Quaternion.Quaternion
        Dim index As Integer
        Dim interpolationFactor As Single

        '// Get previous and next quaternions
        index = animations.GetAnimationFrame(animationName).GetFrame(time, 0)
        prevQ = GetRotationQuaternion(aniSampler, index)
        index = animations.GetAnimationFrame(animationName).GetFrame(time, 1)
        nextQ = GetRotationQuaternion(aniSampler, index)

        '// Interpolate between quaternions
        interpolationFactor = AnimationFrames.InterpolationFactor(time)
        Return Quaternion.NLerp({prevQ, nextQ}, {1 - interpolationFactor, interpolationFactor})
    End Function

    '// Cast array of singles into a quaternion
    Private Function GetRotationQuaternion(aniSampler As Sampler, index As Integer) As Quaternion.Quaternion
        Dim rotQ As New Quaternion.Quaternion
        rotQ.X = aniSampler.outputData(index * 4)
        rotQ.Y = aniSampler.outputData(index * 4 + 1)
        rotQ.Z = aniSampler.outputData(index * 4 + 2)
        rotQ.W = aniSampler.outputData(index * 4 + 3)
        Return rotQ
    End Function

    '// Cast array of singles into a Coord data structure
    Private Function GetTranslationVector(aniSampler As Sampler, index As Integer) As COORD3Sng
        Dim transV As New COORD3Sng
        transV.x = aniSampler.outputData(index * 3)
        transV.y = aniSampler.outputData(index * 3 + 1)
        transV.z = aniSampler.outputData(index * 3 + 2)
        Return transV
    End Function

    '// Cast array of singles into a Coord data structure
    Private Function GetScaleVector(aniSampler As Sampler, index As Integer) As COORD3Sng
        Dim scaleV As New COORD3Sng
        scaleV.x = aniSampler.outputData(index * 3)
        scaleV.y = aniSampler.outputData(index * 3 + 1)
        scaleV.z = aniSampler.outputData(index * 3 + 2)
        Return scaleV
    End Function

    '// Get rotation matrix corresponding to a single bone
    Private Function GetRotationMatrix(aniSampler As Sampler, time As Single, animationName As String(), weights As Single()) As Matrices
        Dim index As Integer = 0
        Dim numAnimations As Integer = weights.Length
        Dim interpolatedQuaternion As New Quaternion.Quaternion
        Dim quaternions(numAnimations - 1) As Quaternion.Quaternion

        '// Iterate through each animation
        For i = 0 To weights.Length - 1
            quaternions(i) = GetRotationQuaternionInterpolated(aniSampler, time, animationName(i))
        Next

        '// Interpolate between animations
        interpolatedQuaternion = Quaternion.NLerp(quaternions, weights)

        Return Quaternion.QuarternionToMatrix(interpolatedQuaternion)
    End Function

    Private Function GetTranslationMatrix(aniSampler As Sampler, time As Single, animationName As String) As Matrices
        Dim baseMatrix As New Matrices(4, 4, True)
        Dim translation As COORD3Sng = GetTranslationCoord(aniSampler, time, animationName)
        Return Matrices.Translate(translation, baseMatrix)
    End Function

    Private Function GetTranslationCoord(aniSampler As Sampler, time As Single, animationName As String) As COORD3Sng
        Return GetTranslationVector(aniSampler, animations.GetAnimationFrame(animationName).GetFrame(time, 0))
    End Function

    '// Count how many bitmap files exist in the format SKIN.bmp, SKINa.bmp, SKINb.bmp etc.
    Private Function GetNumSkins(path As String) As Integer
        Dim numSkins As Integer = 0
        While IO.File.Exists(Bitmap.TEXTURE_DIRECTORY & path & Chr(Asc("a"c) + numSkins) & ".bmp")
            numSkins += 1
        End While
        Return numSkins
    End Function

    '// Load texture to a buffer on the GPU
    Private Function LoadTextures(path As String, program As Int32) As UInt32()
        Dim numSkins As Integer = GetNumSkins(path)
        '// Initialise pointers and buffers
        Dim texPtr As IntPtr = Marshal.AllocHGlobal(16 + 4 * numSkins)
        Dim texAlbedo, texNormal, texMetal, texSpecular As UInt32
        glGenTextures(4 + numSkins, texPtr)
        texAlbedo = CUInt(Marshal.ReadInt32(texPtr))
        texNormal = CUInt(Marshal.ReadInt32(texPtr, 4))
        texMetal = CUInt(Marshal.ReadInt32(texPtr, 8))
        texSpecular = CUInt(Marshal.ReadInt32(texPtr, 12))

        ReDim texturesAlbedo(numSkins)

        glActiveTexture(GL_TEXTURE_0)

        '// Load albedo (diffuse) textures
        '// There may be multiple skins to load
        TextureLoader.LoadTexture2D(path, texAlbedo, GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)
        texturesAlbedo(0) = texAlbedo
        For i = 0 To numSkins - 1
            texturesAlbedo(i + 1) = CUInt(Marshal.ReadInt32(texPtr, 16 + i * 4))
            TextureLoader.LoadTexture2D(path & Chr(Asc("a"c) + i), texturesAlbedo(i + 1), GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)
        Next

        '// Set sampler reference
        context.glBindTexture(GL_TEXTURE_0, texAlbedo)
        glUniform1i(glGetUniformLocationStr(program, "albedo"), 0)

        '// Load normal, metal and specular maps
        TextureLoader.LoadTexture2D(path & "_normal", texNormal, GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)
        context.glBindTexture(GL_TEXTURE_0 + 1, texNormal)
        glUniform1i(glGetUniformLocationStr(program, "normalMap"), 1)

        TextureLoader.LoadTexture2D(path & "_metal", texMetal, GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)
        context.glBindTexture(GL_TEXTURE_0 + 2, texMetal)
        glUniform1i(glGetUniformLocationStr(program, "metalMap"), 2)

        TextureLoader.LoadTexture2D(path & "_specular", texSpecular, GL_LINEAR_MIPMAP_LINEAR, GL_LINEAR, True)
        context.glBindTexture(GL_TEXTURE_0 + 3, texSpecular)
        glUniform1i(glGetUniformLocationStr(program, "specularMap"), 3)

        '// Free memory and return buffers
        Marshal.FreeHGlobal(texPtr)
        Return {texAlbedo, texNormal, texMetal, texSpecular}
    End Function

    '// Generate vertex array and enable all attributes
    Private Function VertexArray() As UInt32
        Dim VAOPtr As IntPtr = Marshal.AllocHGlobal(4)
        Dim VAO As UInt32
        glGenVertexArrays(1, VAOPtr)
        VAO = CUInt(Marshal.ReadInt32(VAOPtr))

        glBindFramebuffer(GL_FRAMEBUFFER, 0)
        context.glBindVertexArray(VAO)
        For i = 0 To 5
            glEnableVertexAttribArray(CUInt(i))
        Next
        Marshal.FreeHGlobal(VAOPtr)
        Return VAO
    End Function

    '// Create new model from a file
    Sub New(inModelName As String, program As Int32, ByRef inContext As OpenGLContext)
        Dim rawData As String
        Dim JSONData As JSON.JSONObject
        Dim isSkinned As Boolean

        modelName = inModelName

        '// Set reference to OpenGL context and use GLTF program
        context = inContext
        context.glUseProgram(CUInt(program))

        '// Load buffers
        textures = LoadTextures(modelName, program)
        VAOShadow = VertexArray()
        VAO = VertexArray()

        '// Get raw data and parse to JSON object
        rawData = IO.File.ReadAllText(MODEL_DIRECTORY & modelName & ".gltf")
        animations = New AnimationFrames(modelName)
        JSONData = JSON.GetJSON(rawData)

        '// Extract data from JSON
        buffers = GetBuffers(JSONData)
        bufferViews = GetBufferViews(JSONData)
        accessors = GetAccessors(JSONData)
        meshes = GetMeshes(JSONData)
        nodes = GetNodes(JSONData)
        rootNode = GetParentNode(nodes)
        samplers = GetSamplers(JSONData)
        channels = GetChannels(JSONData)
        jointsMapping = GetJoints(JSONData)
        RemapJoints()
        LoadSamplerData(samplers, accessors)

        '// Link skins and animations to nodes
        For i = 0 To channels.Length - 1
            nodes(channels(i).node).AssignAnimations(channels(i))
        Next
        isSkinned = JSONData.GetPairs("skins").Length > 0
        If isSkinned Then LoadInverseBindMatrices(JSONData.GetPairs("skins")(0).Properties, accessors)
        InitialiseBuffers()
    End Sub

    '// Extract joint data
    Private Function GetJoints(ByRef data As JSON.JSONObject) As Int32()
        If data.GetPairs("skins").Length > 0 Then
            Dim jointMapStr As String() = data.GetPairs("skins")(0).Properties(0).GetPairs("joints")(0).Value
            Dim jointMap(jointMapStr.Length - 1) As Integer
            For i = 0 To jointMap.Length - 1
                jointMap(i) = CInt(jointMapStr(i))
            Next
            Return jointMap
        Else
            Return {}
        End If
    End Function

    Private Sub RemapJoints()
        Dim jointsAccessor As Accessor = GetJoints(MESH_CONST)
        Dim jointData(jointsAccessor.count - 1) As Integer

        If jointsAccessor.count <> 0 Then
            If jointsAccessor.count * 4 = jointsAccessor.data.Length Then '// 8 bit
                RemapJointsByte(jointsAccessor, jointsMapping)
            ElseIf jointsAccessor.count * 8 = jointsAccessor.data.Length Then '// 16 bit
                RemapJointsShort(jointsAccessor, jointsMapping)
            Else '// 32 bit
                RemapJointsInteger(jointsAccessor, jointsMapping)
            End If
        End If
    End Sub

    Private Sub RemapJointsByte(jointAccessor As Accessor, jointMapping As Integer())
        For i = 0 To jointAccessor.data.Length - 1
            jointAccessor.data(i) = CByte(jointMapping(jointAccessor.data(i)))
        Next
        accessors(meshes(MESH_CONST).combinedPrimitives.joints).data = jointAccessor.data
    End Sub

    Private Sub RemapJointsShort(jointAccessor As Accessor, jointMapping As Integer())
        Dim dataShort(jointAccessor.data.Length \ 2 - 1) As Short
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(jointAccessor.data.Length)
        Marshal.Copy(jointAccessor.data, 0, dataPtr, jointAccessor.data.Length)
        Marshal.Copy(dataPtr, dataShort, 0, dataShort.Length)
        For i = 0 To dataShort.Length - 1
            dataShort(i) = CShort(jointMapping(dataShort(i)))
        Next
        Marshal.Copy(dataShort, 0, dataPtr, dataShort.Length)
        Marshal.Copy(dataPtr, accessors(meshes(MESH_CONST).combinedPrimitives.joints).data, 0, jointAccessor.data.Length)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    Private Sub RemapJointsInteger(jointAccessor As Accessor, jointMapping As Integer())
        Dim dataInteger(jointAccessor.data.Length \ 4 - 1) As Integer
        Dim dataPtr As IntPtr = Marshal.AllocHGlobal(jointAccessor.data.Length)
        Marshal.Copy(jointAccessor.data, 0, dataPtr, jointAccessor.data.Length)
        Marshal.Copy(dataPtr, dataInteger, 0, dataInteger.Length)
        For i = 0 To dataInteger.Length - 1
            dataInteger(i) = jointMapping(dataInteger(i))
        Next
        Marshal.Copy(dataInteger, 0, dataPtr, dataInteger.Length)
        Marshal.Copy(dataPtr, accessors(meshes(MESH_CONST).combinedPrimitives.joints).data, 0, jointAccessor.data.Length)
        Marshal.FreeHGlobal(dataPtr)
    End Sub

    '// Returns index of root node
    Private Function GetParentNode(ByRef nodes As Node()) As Integer
        For i = 0 To nodes.Length - 1
            nodes(i).parent = -1
        Next
        For i = 0 To nodes.Length - 1
            For j = 0 To nodes(i).children.Length - 1
                nodes(nodes(i).children(j)).parent = i
            Next
        Next
        For i = 0 To nodes.Length - 1
            If nodes(i).name = "root" Or nodes(i).name = """root""" Then
                Return i
            End If
        Next
        Return -1
    End Function

    '// Add reference to parent node index
    '// Used for faster traversal up the tree
    Private Function GetParents(nodes As Node()) As Integer()
        Dim parents(nodes.Length - 1) As Integer
        For i = 0 To nodes.Length - 1
            parents(i) = nodes(i).parent
        Next
        Return parents
    End Function

    '// Multiply child matrices by its parents so transforms are applied recursively down the tree
    Private Sub ApplyRecursiveMatrix(ByRef matrixArr As Matrices(), ByRef parents As Integer(), ByRef applied As Boolean(), toapply As Integer)
        If Not applied(toapply) Then
            '// Different leaves may lead to a node that has already been multiplied
            '// If this is the case do not multiply it twice as this will have the wrong matrix
            If parents(toapply) = -1 Then
                '// Base case, do nothing if this is root
                applied(toapply) = True
            Else
                '// Calculate parent's matrix recursively
                ApplyRecursiveMatrix(matrixArr, parents, applied, parents(toapply))
                '// Transform matrix by matrix of parent
                matrixArr(toapply) = Matrices.Multiply(matrixArr(parents(toapply)), matrixArr((toapply)))
                applied(toapply) = True
            End If
        End If
    End Sub

    '// Used to combine data from multiple meshes into one accessor
    Private Function MergeAccessors(accessor1 As Accessor, accessor2 As Accessor) As Accessor
        '// Create new accessor
        Dim newAccessor As New Accessor()
        newAccessor.componentType = accessor1.componentType
        newAccessor.count = accessor1.count + accessor2.count
        newAccessor.type = accessor1.type
        ReDim newAccessor.data(accessor1.data.Length + accessor2.data.Length - 1)
        '// Copy data to new accessor
        Array.Copy(accessor1.data, 0, newAccessor.data, 0, accessor1.data.Length)
        Array.Copy(accessor2.data, 0, newAccessor.data, accessor1.data.Length, accessor2.data.Length - 1)
        Return newAccessor
    End Function

    '// Load all accessors and copy data to GPU buffers
    Private Sub LoadBufferData()
        Dim positionAccessor, normalAccessor, weightAccessor, jointAccessor, textureAccessor, indexAccessor As Accessor
        Dim tangentData As Byte()

        glGenBuffers(6, buffersPtr)

        '// Get accessors with data
        positionAccessor = GetPosition(MESH_CONST)
        normalAccessor = GetNormal(MESH_CONST)
        weightAccessor = GetWeights(MESH_CONST)
        jointAccessor = GetJoints(MESH_CONST)
        textureAccessor = GetTextureCoords(MESH_CONST)
        indexAccessor = GetIndices(MESH_CONST)

        If jointAccessor.count = 0 Then
            '// Not rigged, assign default rigging
            ReDim jointAccessor.data(positionAccessor.count * 4 - 1)
            ReDim weightAccessor.data(positionAccessor.count * 16 - 1)
            For i = 0 To positionAccessor.count - 1
                '// Set first weight to 1 (so it only considers first bone)
                weightAccessor.data(i * 16 + 2) = 128
                weightAccessor.data(i * 16 + 3) = 63
            Next
            For i = 0 To jointAccessor.data.Length - 1
                '// Assign default joints
                If jointAccessor.data(i) <> 0 Then
                    jointAccessor.data(i) = 0
                End If
            Next
            jointAccessor.count = positionAccessor.count
            weightAccessor.count = positionAccessor.count
            jointAccessor.componentType = GL_UNSIGNED_BYTE
            weightAccessor.componentType = GL_FLOAT
        End If

        '// Assign default texture coordinates
        If textureAccessor.count = 0 Then
            textureAccessor.count = positionAccessor.count
            ReDim textureAccessor.data(textureAccessor.count * 8)
        End If

        '// Get buffer indices
        bufferPosition = CUInt(Marshal.ReadInt32(buffersPtr))
        bufferWeights = CUInt(Marshal.ReadInt32(buffersPtr + 4))
        bufferNormals = CUInt(Marshal.ReadInt32(buffersPtr + 8))
        bufferJoints = CUInt(Marshal.ReadInt32(buffersPtr + 12))
        bufferTexture = CUInt(Marshal.ReadInt32(buffersPtr + 16))
        bufferTangents = CUInt(Marshal.ReadInt32(buffersPtr + 20))

        tangentData = GenerateTangents(textureAccessor.data, positionAccessor.data, indexAccessor.data, indexAccessor.componentType)

        '// Write data to buffer
        OpenGLWrapper.BufferData(positionAccessor.data, GL_ARRAY_BUFFER, bufferPosition)
        OpenGLWrapper.BufferData(normalAccessor.data, GL_ARRAY_BUFFER, bufferNormals)
        OpenGLWrapper.BufferData(tangentData, GL_ARRAY_BUFFER, bufferTangents)
        OpenGLWrapper.BufferData(weightAccessor.data, GL_ARRAY_BUFFER, bufferWeights)
        OpenGLWrapper.BufferData(jointAccessor.data, GL_ARRAY_BUFFER, bufferJoints)
        OpenGLWrapper.BufferData(textureAccessor.data, GL_ARRAY_BUFFER, bufferTexture)

        '// Bind program attribute index to the buffer index
        OpenGLWrapper.BindBufferToProgramAttributes(bufferPosition, 3, positionAccessor.componentType, 0, OpenGLWrapper.VertexType.FLOAT)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferWeights, 4, weightAccessor.componentType, 1, OpenGLWrapper.VertexType.FLOAT)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferNormals, 3, normalAccessor.componentType, 2, OpenGLWrapper.VertexType.FLOAT)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferTangents, 3, GL_FLOAT, 3, OpenGLWrapper.VertexType.FLOAT)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferJoints, 4, jointAccessor.componentType, 4, OpenGLWrapper.VertexType.INT)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferTexture, 2, textureAccessor.componentType, 5, OpenGLWrapper.VertexType.FLOAT)

        '// Bind shadow map program attributes to buffer indices
        '// Normals and textures are not required for shadows
        context.glBindVertexArray(VAOShadow)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferPosition, 3, positionAccessor.componentType, 0, OpenGLWrapper.VertexType.FLOAT)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferWeights, 4, weightAccessor.componentType, 1, OpenGLWrapper.VertexType.FLOAT)
        OpenGLWrapper.BindBufferToProgramAttributes(bufferJoints, 4, jointAccessor.componentType, 2, OpenGLWrapper.VertexType.INT)

        context.glBindVertexArray(VAO)
    End Sub

    '// Generate tangent vectors for use in normal mapping
    Private Function GenerateTangents(textureByte As Byte(), positionByte As Byte(), indices As Byte(), indexType As UInteger) As Byte()
        Dim tangentByte(positionByte.Length - 1) As Byte
        Dim tangentSng(positionByte.Length \ 4 - 1) As Single
        Dim textureSng As Single()
        Dim positionSng As Single()
        Dim indexInt As Integer()
        Dim pos(2) As COORD3Sng
        Dim texPos(2) As COORD2Sng
        Dim maxIndex As Integer = 0

        '// Get arrays of cast data
        textureSng = BytesToSingle(textureByte)
        positionSng = BytesToSingle(positionByte)
        indexInt = BytesToInteger(indices, indexType)

        For i = 0 To indexInt.Length - 1 Step 3
            '// Iterate through each triangle
            For j = 0 To 2
                '// For each vertex get the 3d position and the position of the texture
                pos(j).x = positionSng(indexInt(i + j) * 3)
                pos(j).y = positionSng(indexInt(i + j) * 3 + 1)
                pos(j).z = positionSng(indexInt(i + j) * 3 + 2)
                texPos(j).x = textureSng(indexInt(i + j) * 2)
                texPos(j).y = textureSng(indexInt(i + j) * 2 + 1)
            Next
            '// Get a tangent for each triangle
            CalculateTangent(pos, texPos, tangentSng, {indexInt(i), indexInt(i + 1), indexInt(i + 2)})
        Next

        '// Control the magnitude of the tangent
        For i = 0 To tangentSng.Length - 1
            tangentSng(i) /= 10
        Next

        Return SingleToBytes(tangentSng)
    End Function

    '// Get tangent vector in model space from texture space
    Private Sub CalculateTangent(worldPos As COORD3Sng(), texturePos As COORD2Sng(), ByRef tangent As Single(), vertexIndex As Integer())
        Dim triangleIndices As Integer(,) = {{1, 2}, {0, 2}, {0, 1}}
        Dim deltaWorldPos(1) As COORD3Sng
        Dim deltaTexturePos(1) As COORD2Sng
        Dim tangentInstance As New COORD3Sng

        For i = 0 To 2
            '// Repeat for each vertex
            For j = 0 To 1
                '// Get vectors to other vertices from each vertex
                deltaWorldPos(j).x = 1000 * (worldPos(triangleIndices(i, j)).x - worldPos(i).x)
                deltaWorldPos(j).y = 1000 * (worldPos(triangleIndices(i, j)).y - worldPos(i).y)
                deltaWorldPos(j).z = 1000 * (worldPos(triangleIndices(i, j)).z - worldPos(i).z)
                deltaTexturePos(j).x = 1000 * (texturePos(triangleIndices(i, j)).x - texturePos(i).x)
                deltaTexturePos(j).y = 1000 * (texturePos(triangleIndices(i, j)).y - texturePos(i).y)
            Next
            tangentInstance = CalculateTangent(deltaWorldPos, deltaTexturePos)
            '// Get tangent
            tangent(vertexIndex(i) * 3 + 0) += tangentInstance.x
            tangent(vertexIndex(i) * 3 + 1) += tangentInstance.y
            tangent(vertexIndex(i) * 3 + 2) += tangentInstance.z
            '// Average the tangents
        Next
    End Sub

    '// Returns a vector of tangents in world space
    Private Function CalculateTangent(worldPos As COORD3Sng(), texturePos As COORD2Sng()) As COORD3Sng
        Dim tangent As New COORD3Sng
        tangent.x = CalculateTangentComponent({worldPos(0).x, worldPos(1).x}, texturePos)
        tangent.y = CalculateTangentComponent({worldPos(0).y, worldPos(1).y}, texturePos)
        tangent.z = CalculateTangentComponent({worldPos(0).z, worldPos(1).z}, texturePos)
        Return tangent
    End Function

    '// Get component of vector in model space
    Private Function CalculateTangentComponent(worldPos As Single(), texturePos As COORD2Sng()) As Single
        Dim numerator As Single
        Dim denominator As Single
        numerator = worldPos(0) * texturePos(1).y - worldPos(1) * texturePos(0).y
        denominator = texturePos(0).x * texturePos(1).y - texturePos(0).y * texturePos(1).x
        If denominator = 0 Then denominator = 0.0001
        '// Prevent division by 0
        Return numerator / denominator
    End Function

    '// Used to view contents of an accessor, for debugging
    Private Sub DebugData(aa As Accessor)
        Dim dataDebugPtr As IntPtr = Marshal.AllocHGlobal(100000)
        Dim debugData(1000) As Single
        Dim debugDataIndex(100) As Single
        Marshal.Copy(aa.data, 0, dataDebugPtr, aa.data.Length)
        Marshal.Copy(dataDebugPtr, debugData, 0, 100)
        For i = 0 To 99
            debugDataIndex(i) = debugData(accessors(61).data(i * 2))
        Next
        Marshal.FreeHGlobal(dataDebugPtr)
    End Sub

    '// Write data to buffers on GPU
    Public Sub InitialiseBuffers()
        Dim bufferIndices As UInt32
        Dim indicesAccessor As Accessor
        Dim vertBuffer As UInt32
        Dim used(GetPosition(MESH_CONST).count - 1) As Boolean
        buffersPtr = Marshal.AllocHGlobal(32)
        LoadBufferData()
        vertBuffer = CUInt(Marshal.ReadInt32(buffersPtr))

        If IsIndexed(MESH_CONST) Then
            '// Some GLTF models have vertices indexed to save space
            glGenBuffers(1, buffersPtr)
            indicesAccessor = GetIndices(MESH_CONST)
            bufferIndices = CUInt(Marshal.ReadInt32(buffersPtr))
            Dim max As Integer = 0
            '// Get largest index
            For i = 0 To indicesAccessor.data.Length - 1
                If indicesAccessor.data(i) > max Then max = indicesAccessor.data(i)
            Next
            indexType = indicesAccessor.componentType
            OpenGLWrapper.BufferData(indicesAccessor.data, GL_ELEMENT_ARRAY_BUFFER, bufferIndices)
        Else
            '// Assume a model has indices
            WriteLine("Model is bad")
            ReadLine()
        End If

        context.glBindVertexArray(VAOShadow)
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, bufferIndices)
        context.glBindVertexArray(VAO)
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, bufferIndices)
        numVertices = CUInt(indicesAccessor.data.Length)
    End Sub

    '// Copy matrix data to uniforms
    Public Sub GLTFMatrices(time As Single, GLTFProgram As Int32, matrixRelative As Matrices, matrixView As Matrices, matrixPersepective As Matrices, matrixModel As Matrices, matrixNormal As Matrices, animationName As String, animationStartTime As Single, fadeOut As Mob.AnimationQueueItem(), transformationMatricesInverse As Matrices())
        Dim transformationMatrices As Matrices() = transformationMatricesInverse
        Dim matrixPtr As IntPtr = Marshal.AllocHGlobal(transformationMatrices.Length * 64)
        glUniformMatrix4fv(glGetUniformLocationStr(GLTFProgram, "perspectiveMatrix"), 1, False, matrixPersepective.ToOpenGLMatrix())
        glUniformMatrix4fv(glGetUniformLocationStr(GLTFProgram, "viewMatrix"), 1, False, matrixView.ToOpenGLMatrix())
        glUniformMatrix4fv(glGetUniformLocationStr(GLTFProgram, "relativeMatrix"), 1, False, matrixRelative.ToOpenGLMatrix())
        glUniformMatrix4fv(glGetUniformLocationStr(GLTFProgram, "modelMatrix"), 1, False, matrixModel.ToOpenGLMatrix())
        Matrices.MatrixArrayToPtr(matrixPtr, transformationMatrices)
        glUniformMatrix4fv(glGetUniformLocationStr(GLTFProgram, "animateMatrix"), transformationMatrices.Length, False, matrixPtr)
        glUniformMatrix3fv(glGetUniformLocationStr(GLTFProgram, "normalMatrix"), 1, False, matrixNormal.ToOpenGLMatrix())
        Marshal.FreeHGlobal(matrixPtr)
    End Sub

    Public Sub GLTFMatrices(time As Single, program As Int32, matrixModel As Matrices, animationName As String, animationStartTime As Single, fadeOut As Mob.AnimationQueueItem(), transformationMatricesInverse As Matrices())
        Dim transformationMatrices As Matrices() = transformationMatricesInverse
        Dim matrixPtr As IntPtr = Marshal.AllocHGlobal(transformationMatrices.Length * 64)
        glUniformMatrix4fv(glGetUniformLocationStr(program, "modelMatrix"), 1, False, matrixModel.ToOpenGLMatrix())
        Matrices.MatrixArrayToPtr(matrixPtr, transformationMatrices)
        glUniformMatrix4fv(glGetUniformLocationStr(program, "animateMatrix"), transformationMatrices.Length, False, matrixPtr)
        Marshal.FreeHGlobal(matrixPtr)
    End Sub

    Private Function RoundMatrices(m As Matrices()) As Matrices()
        For i = 0 To m.Length - 1
            For j = 0 To m(i).data.Length - 1
                m(i).data(j) = CSng(Math.Round(m(i).data(j), 1))
            Next
        Next
        Return m
    End Function

    '// Get animations
    Private Function GetChannels(JSONData As JSON.JSONObject) As Channel()
        Dim channelJSON As JSON.JSONPropertyValue
        Dim channelLst As New List(Of Channel)

        If JSONData.GetPairs("animations").Length > 0 Then
            channelJSON = JSONData.GetPairs("animations")(0).Properties(0).GetPairs("channels")(0)
            For i = 0 To channelJSON.Properties.Length - 1
                channelLst.Add(New Channel(channelJSON.Properties(i)))
            Next
        End If
        Return channelLst.ToArray()
    End Function

    '// Load samplers from JSON
    Private Function GetSamplers(JSONData As JSON.JSONObject) As Sampler()
        Dim samplerJSON As JSON.JSONPropertyValue
        Dim samplerLst As New List(Of Sampler)

        If JSONData.GetPairs("animations").Length > 0 Then
            samplerJSON = JSONData.GetPairs("animations")(0).Properties(0).GetPairs("samplers")(0)
            For i = 0 To samplerJSON.Properties.Length - 1
                samplerLst.Add(New Sampler(samplerJSON.Properties(i)))
            Next
        End If
        Return samplerLst.ToArray()
    End Function

    '// Each input data is mapped to output data
    '// For example, at 0.2 seconds, index of input data with value 0.2 is used to get output position
    Private Sub LoadSamplerData(ByRef samplers As Sampler(), ByRef accessors As Accessor())
        For i = 0 To samplers.Length - 1
            samplers(i).inputData = BytesToSingle(accessors(samplers(i).input).data)
            samplers(i).outputData = BytesToSingle(accessors(samplers(i).output).data)
        Next
    End Sub

    '// Conversion between data types
    Private Function BytesToInteger(ByRef bytes As Byte(), componentType As UInteger) As Integer()
        Dim bytesPerData As Integer
        Dim currentValue As Integer
        Dim integerData As New List(Of Integer)

        Select Case componentType
            Case GL_BYTE
                bytesPerData = 1
            Case GL_UNSIGNED_BYTE
                bytesPerData = 1
            Case GL_INT
                bytesPerData = 4
            Case GL_UNSIGNED_INT
                bytesPerData = 4
            Case GL_UNSIGNED_SHORT
                bytesPerData = 2
            Case Else
                bytesPerData = 1
        End Select

        currentValue = 0
        For i = 0 To bytes.Length - 1
            currentValue += CInt(bytes(i)) << ((i Mod bytesPerData) * 8)
            If (i + 1) Mod bytesPerData = 0 Then
                integerData.Add(currentValue)
                currentValue = 0
            End If
        Next

        Return integerData.ToArray
    End Function

    '// Cast bytes to array of floats using unmanaged memory
    Private Function BytesToSingle(ByRef bytes As Byte()) As Single()
        Dim tempPtr As IntPtr = Marshal.AllocHGlobal(bytes.Length)
        Dim singleData(bytes.Length \ 4 - 1) As Single
        Marshal.Copy(bytes, 0, tempPtr, bytes.Length)
        Marshal.Copy(tempPtr, singleData, 0, singleData.Length)
        Marshal.FreeHGlobal(tempPtr)
        Return singleData
    End Function

    '// Cast array of floats to an array of bytes using unmanaged memory
    Private Function SingleToBytes(ByRef singles As Single()) As Byte()
        Dim tempPtr As IntPtr = Marshal.AllocHGlobal(singles.Length * 4)
        Dim singleData(singles.Length * 4 - 1) As Byte
        Marshal.Copy(singles, 0, tempPtr, singles.Length)
        Marshal.Copy(tempPtr, singleData, 0, singleData.Length)
        Marshal.FreeHGlobal(tempPtr)
        Return singleData
    End Function

    '// Nodes are the objects in a model
    Private Function GetNodes(JSONData As JSON.JSONObject) As Node()
        Dim nodeJSON As JSON.JSONPropertyValue
        Dim nodeLst As New List(Of Node)

        nodeJSON = JSONData.GetPairs("nodes")(0)
        For i = 0 To nodeJSON.Properties.Length - 1
            nodeLst.Add(New Node(nodeJSON.Properties(i)))
        Next
        Return nodeLst.ToArray()
    End Function

    '// Meshes contain vertex data such as position and texture
    Private Function GetMeshes(JSONData As JSON.JSONObject) As Mesh()
        Dim meshJSON As JSON.JSONPropertyValue
        Dim meshLst As New List(Of Mesh)
        Dim meshInstance As New Mesh

        meshJSON = JSONData.GetPairs("meshes")(0)
        For i = 0 To meshJSON.Properties.Length - 1
            meshInstance = New Mesh(meshJSON.Properties(i))
            GenerateCombinedPrimitives(meshInstance)
            'meshLst.Add(New Mesh(meshJSON.Properties(i)))
            meshLst.Add(meshInstance)
        Next
        Return meshLst.ToArray()
    End Function

    '// Used to combine multiple mesh objects into one
    Private Function MergeIndicesAccessor(data As Integer(), newIndices As Accessor, offset As Integer) As Integer()
        Dim combinedData As Integer()
        Dim newData As Integer() = BytesToInteger(newIndices.data, newIndices.componentType)

        '// Offset is required because the indices no longer refer to the same data
        For i = 0 To newData.Length - 1
            newData(i) += offset
        Next

        '// Combine index data
        ReDim combinedData(data.Length + newData.Length - 1)
        Array.Copy(data, combinedData, data.Length)
        Array.Copy(newData, 0, combinedData, data.Length, newData.Length)

        Return combinedData
    End Function

    '// Used to merge primitives so one model can display them all
    Private Sub GenerateCombinedPrimitives(ByRef meshInstance As Mesh)
        Dim newIndices As New Accessor(True)
        Dim newJoints As New Accessor(True)
        Dim newNormals As New Accessor(True)
        Dim newPositions As New Accessor(True)
        Dim newTextures As New Accessor(True)
        Dim newWeights As New Accessor(True)
        Dim animated As Boolean = True
        Dim newIndicesData As Integer() = {}
        Dim newIndexPtr As IntPtr

        '// If a model is not animated, set flag
        If meshInstance.primitives(0).joints = -1 Then
            animated = False
        End If

        '// Copy component type e.g. floating point number
        newIndices.componentType = accessors(meshInstance.primitives(0).indices).componentType
        newPositions.componentType = accessors(meshInstance.primitives(0).position).componentType
        newNormals.componentType = accessors(meshInstance.primitives(0).normal).componentType
        newTextures.componentType = accessors(meshInstance.primitives(0).texture).componentType
        If animated Then
            newJoints.componentType = accessors(meshInstance.primitives(0).joints).componentType
            newWeights.componentType = accessors(meshInstance.primitives(0).weights).componentType
        End If

        '// COmbine raw data from each primitive into one byte array
        For i = 0 To meshInstance.primitives.Length - 1
            newIndicesData = MergeIndicesAccessor(newIndicesData, accessors(meshInstance.primitives(i).indices), newPositions.count)
            If animated Then
                newWeights = MergeAccessors(newWeights, accessors(meshInstance.primitives(i).weights))
                newJoints = MergeAccessors(newJoints, accessors(meshInstance.primitives(i).joints))
            End If
            newNormals = MergeAccessors(newNormals, accessors(meshInstance.primitives(i).normal))
            newPositions = MergeAccessors(newPositions, accessors(meshInstance.primitives(i).position))
            newTextures = MergeAccessors(newTextures, accessors(meshInstance.primitives(i).texture))
        Next

        '// Create new index buffer
        ReDim newIndices.data(newIndicesData.Length * 4)
        newIndexPtr = Marshal.AllocHGlobal(newIndicesData.Length * 4)
        Marshal.Copy(newIndicesData, 0, newIndexPtr, newIndicesData.Length)
        Marshal.Copy(newIndexPtr, newIndices.data, 0, newIndices.data.Length)
        Marshal.FreeHGlobal(newIndexPtr)
        newIndices.count = newIndicesData.Length
        newIndices.componentType = GL_UNSIGNED_INT

        '// Create new accessors with the combined data
        If animated Then
            ReDim Preserve accessors(accessors.Length + 5)
        Else
            ReDim Preserve accessors(accessors.Length + 3)
        End If
        accessors(accessors.Length - 1) = newIndices
        accessors(accessors.Length - 2) = newTextures
        accessors(accessors.Length - 3) = newNormals
        accessors(accessors.Length - 4) = newPositions
        If animated Then
            accessors(accessors.Length - 5) = newJoints
            accessors(accessors.Length - 6) = newWeights
        End If

        '// Add a reference to the new accessors
        meshInstance.combinedPrimitives.indices = accessors.Length - 1
        meshInstance.combinedPrimitives.texture = accessors.Length - 2
        meshInstance.combinedPrimitives.normal = accessors.Length - 3
        meshInstance.combinedPrimitives.position = accessors.Length - 4
        If animated Then
            meshInstance.combinedPrimitives.joints = accessors.Length - 5
            meshInstance.combinedPrimitives.weights = accessors.Length - 6
        End If
    End Sub

    '// Accessors contain information about the type of data stored in buffers
    Private Function GetAccessors(JSONData As JSON.JSONObject) As Accessor()
        Dim accessorJSON As JSON.JSONPropertyValue
        Dim accessorLst As New List(Of Accessor)

        accessorJSON = JSONData.GetPairs("accessors")(0)
        For i = 0 To accessorJSON.Properties.Length - 1
            accessorLst.Add(New Accessor(accessorJSON.Properties(i), bufferViews))
        Next
        Return accessorLst.ToArray()
    End Function

    '// Buffer views represent a section of the binary file
    Private Function GetBufferViews(JSONData As JSON.JSONObject) As BufferView()
        Dim bufferViewJSON As JSON.JSONPropertyValue
        Dim bufferViewLst As New List(Of BufferView)

        bufferViewJSON = JSONData.GetPairs("bufferViews")(0)
        For i = 0 To bufferViewJSON.Properties.Length - 1
            bufferViewLst.Add(New BufferView(bufferViewJSON.Properties(i), buffers))
        Next
        Return bufferViewLst.ToArray()
    End Function

    '// Buffers contian the data in a binary file
    Private Function GetBuffers(JSONData As JSON.JSONObject) As Buffer()
        Dim buffersJSON As JSON.JSONPropertyValue
        Dim buffersLst As New List(Of Buffer)

        buffersJSON = JSONData.GetPairs("buffers")(0)
        For i = 0 To buffersJSON.Properties.Length - 1
            buffersLst.Add(New Buffer(buffersJSON.Properties(i)))
        Next
        Return buffersLst.ToArray()
    End Function

    '// Bones are moved into local space before transformations
    '// An inverse matrix is used for this
    Private Sub LoadInverseBindMatrices(skin As JSON.JSONObject(), accessors As Accessor())
        Dim matrixData As Accessor = accessors(CInt(skin(0).GetPairs("inverseBindMatrices")(0).Value(0)))
        Dim matrixDataPtr As IntPtr = Marshal.AllocHGlobal(matrixData.data.Length)
        Dim inverseBindMatrices(nodes.Length - 1) As Matrices
        Dim recursionApplied(nodes.Length - 1) As Boolean

        Marshal.Copy(matrixData.data, 0, matrixDataPtr, matrixData.data.Length)

        For i = 0 To nodes.Length - 1
            inverseBindMatrices(i) = New Matrices(4, 4, True)
        Next

        For i = 0 To jointsMapping.Length - 1
            inverseBindMatrices(jointsMapping(i)) = GetInverseBindMat(matrixDataPtr, 64 * i)
        Next

        For i = 0 To nodes.Length - 1
            nodes(i).inverseBindMatrix = inverseBindMatrices(i)
        Next
    End Sub

    '// Get the inverse bind matrix from byte array
    Private Function GetInverseBindMat(matrixData As IntPtr, offset As Integer) As Matrices
        Dim matrix As New Matrices(4, 4, True)
        Marshal.Copy(matrixData + offset, matrix.data, 0, 16)
        '// GLTF files store matrices in column order, so convert it
        matrix.Transpose()
        Return matrix
    End Function

    '// Structure declaration

    Structure Mesh
        Public name As String
        Public primitives As Primitive()
        Public combinedPrimitives As Primitive
        Sub New(meshObject As JSON.JSONObject)
            Dim primitiveLst As New List(Of Primitive)
            Dim primitiveObjects As JSON.JSONObject() = meshObject.GetPairs("primitives")(0).Properties

            For i = 0 To primitiveObjects.Length - 1
                primitiveLst.Add(New Primitive(primitiveObjects(i)))
            Next
            primitives = primitiveLst.ToArray()
            name = meshObject.GetPairs("name")(0).Value(0)

            combinedPrimitives = New Primitive()
            combinedPrimitives.indexed = primitives(0).indexed
        End Sub
    End Structure

    Structure Primitive
        Public position As Integer
        Public normal As Integer
        Public indices As Integer
        Public weights As Integer
        Public joints As Integer
        Public indexed As Boolean
        Public texture As Integer
        Sub New(primitiveObject As JSON.JSONObject)
            Dim attributes As JSON.JSONObject
            If primitiveObject.GetPairs("indices").Length > 0 Then
                indices = CInt(primitiveObject.GetPairs("indices")(0).Value(0))
                indexed = True
            Else
                indexed = False
            End If
            attributes = primitiveObject.GetPairs("attributes")(0).Properties(0)
            position = CInt(attributes.GetPairs("POSITION")(0).Value(0))
            normal = CInt(attributes.GetPairs("NORMAL")(0).Value(0))

            TryAssignInt("WEIGHTS_0", weights, attributes)
            TryAssignInt("JOINTS_0", joints, attributes)
            TryAssignInt("TEXCOORD_0", texture, attributes)
        End Sub
    End Structure

    Structure Accessor
        Public data As Byte()
        Public componentType As UInt32
        Public count As Integer
        Public type As String
        Sub New(Initialise As Boolean)
            data = {}
        End Sub
        Sub New(accessorObject As JSON.JSONObject, bufferViews As BufferView())
            Dim bufferView As Integer = CInt(accessorObject.GetPairs("bufferView")(0).Value(0))
            componentType = CUInt(accessorObject.GetPairs("componentType")(0).Value(0))
            count = CInt(accessorObject.GetPairs("count")(0).Value(0))
            type = accessorObject.GetPairs("type")(0).Value(0)
            type = type.Substring(1, type.Length - 2)
            data = bufferViews(bufferView).data
        End Sub
    End Structure

    Structure Channel
        Public sampler As Integer
        Public node As Integer
        Public type As AnimationType
        Sub New(channelObject As JSON.JSONObject)
            sampler = CInt(channelObject.GetPairs("sampler")(0).Value(0))
            node = CInt(channelObject.GetPairs("target")(0).Properties(0).GetPairs("node")(0).Value(0))
            Select Case channelObject.GetPairs("target")(0).Properties(0).GetPairs("path")(0).Value(0)
                Case """translation"""
                    type = AnimationType.TRANSLATION
                Case """rotation"""
                    type = AnimationType.ROTATION
                Case """scale"""
                    type = AnimationType.SCALE
            End Select

        End Sub

        Enum AnimationType
            TRANSLATION = 0
            ROTATION = 1
            SCALE = 2
        End Enum
    End Structure

    Structure Sampler
        Public input As Integer
        Public output As Integer
        Public inputData As Single()
        Public outputData As Single()
        Sub New(samplerObject As JSON.JSONObject)
            input = CInt(samplerObject.GetPairs("input")(0).Value(0))
            output = CInt(samplerObject.GetPairs("output")(0).Value(0))
        End Sub
    End Structure

    Structure BufferView
        Public data As Byte()
        Sub New(bufferViewObject As JSON.JSONObject, buffers As Buffer())
            Dim length As Integer = CInt(bufferViewObject.GetPairs("byteLength")(0).Value(0))
            Dim offset As Integer = CInt(bufferViewObject.GetPairs("byteOffset")(0).Value(0))
            Dim buffer As Integer = CInt(bufferViewObject.GetPairs("buffer")(0).Value(0))
            ReDim data(length - 1)
            Array.Copy(buffers(buffer).data, offset, data, 0, length)
        End Sub
    End Structure

    Structure Buffer
        Public data As Byte()
        Sub New(bufferObject As JSON.JSONObject)
            Dim uri As String = bufferObject.GetPairs("uri")(0).Value(0)
            If uri(0) = """" Then
                uri = uri.Substring(1, uri.Length - 2)
            End If
            data = IO.File.ReadAllBytes(MODEL_DIRECTORY & uri)
        End Sub
    End Structure

    Structure Node
        Public name As String
        Public rotation As Quaternion.Quaternion
        Public scale As Single()
        Public translation As Single()
        Public matrix As Single()
        Public hasMatrix As Boolean
        Public children As Integer()
        Public parent As Integer
        Public meshID As Integer
        Public skinID As Integer
        Public rotationAnimation As Integer
        Public scaleAnimation As Integer
        Public translationAnimation As Integer
        Public inverseBindMatrix As Matrices
        Sub New(nodeObject As JSON.JSONObject)
            Dim childrenLst As New List(Of Integer)
            Dim tempChildren As JSON.JSONPropertyValue()
            ReDim scale(2)
            ReDim translation(2)
            ReDim matrix(15)

            name = nodeObject.GetPairs("name")(0).Value(0)
            AssignTransformations("rotation", rotation, nodeObject)
            If Not AssignTransformations("scale", scale, nodeObject) Then scale = {1, 1, 1}
            AssignTransformations("translation", translation, nodeObject)
            hasMatrix = AssignTransformations("matrix", matrix, nodeObject)

            TryAssignInt("mesh", meshID, nodeObject)
            TryAssignInt("skin", skinID, nodeObject)

            tempChildren = nodeObject.GetPairs("children")
            If tempChildren.Length > 0 Then
                For i = 0 To tempChildren(0).Value.Length - 1
                    childrenLst.Add(CInt(tempChildren(0).Value(i)))
                Next
            End If
            children = childrenLst.ToArray()

            translationAnimation = -1
            scaleAnimation = -1
            rotationAnimation = -1
            inverseBindMatrix = New Matrices(4, 4, True)
        End Sub

        Private Function AssignTransformations(ByVal transformName As String, ByRef transformArray As Single(), ByRef nodeObject As JSON.JSONObject) As Boolean
            Dim tempArrayHolder As String()
            If nodeObject.GetPairs(transformName).Length > 0 Then
                tempArrayHolder = nodeObject.GetPairs(transformName)(0).Value
                For i = 0 To tempArrayHolder.Length - 1
                    transformArray(i) = CSng(tempArrayHolder(i))
                Next
                Return True
            End If
            Return False
        End Function

        Private Sub AssignTransformations(ByVal transformName As String, ByRef transformArray As Quaternion.Quaternion, ByRef nodeObject As JSON.JSONObject)
            Dim tempArrayHolder As String()
            If nodeObject.GetPairs(transformName).Length > 0 Then
                tempArrayHolder = nodeObject.GetPairs(transformName)(0).Value
                transformArray.W = CSng(tempArrayHolder(3))
                transformArray.X = CSng(tempArrayHolder(0))
                transformArray.Y = CSng(tempArrayHolder(1))
                transformArray.Z = CSng(tempArrayHolder(2))
            End If
        End Sub

        Public Sub AssignAnimations(animationChannel As Channel)
            Dim samplerID As Integer = animationChannel.sampler
            Select Case animationChannel.type
                Case Channel.AnimationType.ROTATION
                    rotationAnimation = samplerID
                Case Channel.AnimationType.TRANSLATION
                    translationAnimation = samplerID
                Case Channel.AnimationType.SCALE
                    scaleAnimation = samplerID
            End Select
        End Sub
    End Structure

    '// Try to get an integer from JSON
    '// Returns -1 if not found
    Private Shared Sub TryAssignInt(ByVal propertyName As String, ByRef targetInt As Integer, ByRef nodeObject As JSON.JSONObject)
        Dim tempArrayHolder As JSON.JSONPropertyValue()
        tempArrayHolder = nodeObject.GetPairs(propertyName)
        If tempArrayHolder.Length = 0 Then
            targetInt = -1
        Else
            targetInt = CInt(tempArrayHolder(0).Value(0))
        End If
    End Sub
End Class

