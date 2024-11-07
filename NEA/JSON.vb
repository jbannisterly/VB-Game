Option Strict On

'// Class to represent a JSON object as a recursively defined structure
Public Class JSON
    '// Removes whitespace as long it is not part of a string
    Private Shared Function ConditionallyRemoveWhitespace(rawData As String) As String
        Dim spacesRemoved As New Text.StringBuilder
        Dim inLiteral As Boolean = False
        For i = 0 To rawData.Length - 1
            '// Invert boolean when speech mark is found
            If rawData(i) = """" Then
                inLiteral = Not inLiteral
            End If
            '// Add if not a space or add unconditionally if in a literal
            If Not (Not inLiteral AndAlso (rawData(i) = " " Or Asc(rawData(i)) = 10 Or Asc(rawData(13)) = 13)) Then
                spacesRemoved.Append(rawData(i))
            End If
        Next
        Return spacesRemoved.ToString()
    End Function

    '// Convert raw JSON text to array
    Private Shared Function ParseJSONArrayOfObjects(noBracketsData As String) As JSONObject()
        '// Spli by commas
        Dim splitData As String() = SplitJSONComma(noBracketsData)
        Dim objectsLst As New List(Of JSONObject)
        For i = 0 To splitData.Length - 1
            '// Add new JSON object for each element
            objectsLst.Add(ParseJSONObject(RemoveBrackets(splitData(i))))
        Next
        Return objectsLst.ToArray()
    End Function

    '// Remove the outside characters from a string
    Private Shared Function RemoveBrackets(data As String) As String
        Return data.Substring(1, data.Length - 2)
    End Function

    '// Create new recrusively defined JSON object from text
    Private Shared Function ParseJSONObject(noBracketsData As String) As JSONObject
        Dim splitData As String() = SplitJSONComma(noBracketsData)
        Dim propertyValues As New List(Of JSONPropertyValue)
        Dim propertyName As String
        Dim propertyValue As String
        For i = 0 To splitData.Length - 1
            '// Ignore blanks
            If splitData(i) <> "" Then
                '// Split into names and values
                propertyName = splitData(i).Split(":"c)(0)
                propertyValue = splitData(i).Substring(propertyName.Length + 1)
                '// Values may be another JSOON object
                Select Case propertyValue(0)
                    '// Value is a JSON object
                    Case "{"c
                        propertyValues.Add(New JSONPropertyValue(propertyName, ParseJSONObject(RemoveBrackets(propertyValue))))
                    '// Value is an array
                    Case "["c
                        If propertyValue(1) = "{" Then
                            '// Add array of objects
                            propertyValues.Add(New JSONPropertyValue(propertyName, ParseJSONArrayOfObjects(RemoveBrackets(propertyValue))))
                        Else
                            '// Add array of values
                            propertyValues.Add(New JSONPropertyValue(propertyName, SplitJSONComma(RemoveBrackets(propertyValue))))
                        End If
                        '// Value is a single value
                    Case Else
                        propertyValues.Add(New JSONPropertyValue(propertyName, propertyValue))
                End Select
            End If
        Next
        Return New JSONObject(propertyValues.ToArray)
    End Function

    '// Check the types of brackets around a JSON object
    Private Shared Function IsJSONObject(data As String) As Boolean
        Return data(0) = "{" And data(data.Length - 1) = "}"
    End Function

    Private Shared Function IsJSONArray(data As String) As Boolean
        Return data(0) = "[" And data(data.Length - 1) = "]"
    End Function

    '// Split JSON using commas as a delimiter, ensuring that commas in embedded objects are ignored
    Private Shared Function SplitJSONComma(data As String) As String()
        Dim depth As Integer = 0
        Dim splitData As New List(Of String)
        Dim splitStart As Integer = 0
        For i = 0 To data.Length - 1
            Select Case data(i)
                Case "["c, "{"c
                    '// Text represents a child
                    depth += 1
                Case "]"c, "}"c
                    '// End of child, so depth is decremented
                    depth -= 1
                Case ","c
                    '// Split text file if the comma is in the highest level
                    If depth = 0 Then
                        splitData.Add(data.Substring(splitStart, i - splitStart))
                        splitStart = i + 1
                    End If
            End Select
        Next
        splitData.Add(data.Substring(splitStart))
        Return splitData.ToArray()
    End Function

    '// Convert string to JSON object
    Public Shared Function GetJSON(rawData As String) As JSONObject
        Return ParseJSONObject(RemoveBrackets(ConditionallyRemoveWhitespace(rawData)))
    End Function

    '// Structure declaration

    '// Represents an array of name value pairs
    Structure JSONObject
        Public propertyValuePairs As JSONPropertyValue()
        Sub New(inPropertyValuePair As JSONPropertyValue)
            ReDim propertyValuePairs(0)
            propertyValuePairs(0) = inPropertyValuePair
        End Sub
        Sub New(inPropertyValuePair As JSONPropertyValue())
            propertyValuePairs = inPropertyValuePair
        End Sub
        '// Search for values by name
        Public Function GetPairs(name As String) As JSONPropertyValue()
            Dim pairsLst As New List(Of JSONPropertyValue)
            Dim toCheck As String
            For i = 0 To propertyValuePairs.Length - 1
                toCheck = propertyValuePairs(i).Name
                If toCheck = name Or toCheck.Substring(1, toCheck.Length - 2) = name Then
                    pairsLst.Add(propertyValuePairs(i))
                End If
            Next
            Return pairsLst.ToArray()
        End Function
    End Structure

    '// Represents a single name value pair
    '// The value may be another JSON object

    Structure JSONPropertyValue
        Public Name As String
        Public Value As String()
        Public Properties As JSONObject()
        Sub New(inName As String, inValue As String)
            Name = inName
            ReDim Value(0)
            Value(0) = inValue
        End Sub
        Sub New(inName As String, inValue As JSONObject)
            Name = inName
            ReDim Properties(0)
            Properties(0) = inValue
        End Sub
        Sub New(inName As String, inValue As JSONObject())
            Name = inName
            Properties = inValue
        End Sub
        Sub New(inName As String, inValue As String())
            Name = inName
            Value = inValue
        End Sub
    End Structure

End Class

