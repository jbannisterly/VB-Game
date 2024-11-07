Option Strict On
Public Class AbstractDataType
    '// High level wrapper for data structures that can be of any type
    Public Class Queue(Of QueueType)
        Private data As QueueType()
        Private hp As Integer
        Private tp As Integer

        Sub New(length As Integer)
            ReDim data(length - 1)
            hp = 0
            tp = length - 1
        End Sub

        Public Function Peek() As QueueType
            Return data((tp + 1 + data.Length) Mod data.Length)
        End Function
        Public Function Dequeue() As QueueType
            If Not IsEmpty() Then
                tp = (tp + 1 + data.Length) Mod data.Length
                Return data(tp)
            End If
            Return data(0)
        End Function
        Public Sub Enqueue(inData As QueueType)
            If Not IsFull() Then
                data(hp) = inData
                hp = (hp + 1 + data.Length) Mod data.Length
            End If
        End Sub
        Public Function IsFull() As Boolean
            Return hp = tp
        End Function
        Public Function IsEmpty() As Boolean
            Return (tp + 1) Mod data.Length = hp
        End Function
    End Class

    Public Class Stack(Of StackType)
        Private data As StackType()
        Private stackPointer As Integer
        Sub New(length As Integer)
            ReDim data(length - 1)
            stackPointer = 0
        End Sub

        Public Function IsFull() As Boolean
            Return stackPointer = data.Length
        End Function
        Public Function IsEmpty() As Boolean
            Return stackPointer = 0
        End Function
        Public Function Pop(ByRef stackValue As StackType) As Boolean
            If IsEmpty() Then Return False
            stackPointer -= 1
            stackValue = data(stackPointer)
            Return True
        End Function
        Public Sub Push(inData As StackType)
            If Not IsFull() Then
                data(stackPointer) = inData
                stackPointer += 1
            End If
        End Sub
    End Class
End Class
