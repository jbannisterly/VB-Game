Option Strict On
'// Class used to sort items in inventory
Public Class Sorts

    '// This merge sort implementation sorts an array of indices based on another array of data
    '// This is becuase the items to be sorted may often be structures, and the data is what to sort by
    '// The indices can be used to access the structures in a sorted fasion
    '// If data was sorted instead of indices, the values of structures would be changed


    '// Divide array into two smaller arrays
    Public Shared Function MergeSort(ByRef values As Double(), ByRef indices As Integer()) As Integer()
        If indices.Length = 1 Then Return indices
        Dim left(indices.Length \ 2 - 1) As Integer
        Dim right(indices.Length - left.Length - 1) As Integer
        Array.Copy(indices, left, left.Length)
        Array.Copy(indices, left.Length, right, 0, right.Length)
        Return MergeSort(values, MergeSort(values, left), MergeSort(values, right))
    End Function

    '// Merge two sorted arrays
    Private Shared Function MergeSort(ByRef values As Double(), ByRef left As Integer(), ByRef right As Integer()) As Integer()
        Dim combined(left.Length + right.Length - 1) As Integer
        Dim lPtr As Integer = 0
        Dim rPtr As Integer = 0

        For i = 0 To combined.Length - 1
            If lPtr >= left.Length Then
                combined(i) = right(rPtr)
                rPtr += 1
            ElseIf rPtr >= right.Length Then
                combined(i) = left(lPtr)
                lPtr += 1
            ElseIf values(left(lPtr)) > values(right(rPtr)) Then
                combined(i) = right(rPtr)
                rPtr += 1
            Else
                combined(i) = left(lPtr)
                lPtr += 1
            End If
        Next

        Return combined
    End Function
End Class

