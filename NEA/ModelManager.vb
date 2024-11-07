Option Strict On
'// Class to reduce the number of times a model is loaded and to store all models
Public Class ModelManager
    Private loadedModels As GLTFModel()
    Private context As OpenGLContext

    '// Add a reference to the OpenGL context
    Sub New(ByRef inContext As OpenGLContext)
        context = inContext
    End Sub

    '// Load all models in a folder
    Public Sub LoadModels(folder As String, program As Integer)
        Dim models As New List(Of GLTFModel)
        Dim modelNames As String() = IO.Directory.GetFiles(folder)
        For i = 0 To modelNames.Length - 1
            '// Extract file name
            modelNames(i) = modelNames(i).Split("\"c)(modelNames(i).Split("\"c).Length - 1)
            modelNames(i) = modelNames(i).Split("."c)(0)
            '// Load model
            models.Add(New GLTFModel(modelNames(i), program, context))
        Next
        loadedModels = models.ToArray()
    End Sub

    '// Linear search for a model
    Public Function GetModel(modelName As String) As GLTFModel
        For i = 0 To loadedModels.Length - 1
            If loadedModels(i).modelName = modelName Then
                '// Model has already been loaded so return the existing model
                Return loadedModels(i)
            End If
        Next
        '// Return a default model
        Return loadedModels(0)
    End Function

End Class

