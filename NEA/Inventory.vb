Option Strict On
'// Manage the collection of items the player has, and its display
Public Class Inventory
    '// Variable declaration
    Public inventoryContents As List(Of InventoryItem)
    Public inventoryDisplay As GUIObject.GridView
    Public itemEncyclopedia As List(Of ItemData)
    Public inventoryDetail As GUIObject.Label
    Public inventoryDetailImage As GUIObject.RenderTarget
    Public selectedItem As ItemInstance
    Public missionList As Missions
    Private context As OpenGLContext
    Private indexLookup As Integer()
    Private indexLookupFiltered As List(Of Integer)
    Private currentFilter As Integer

    '// Initialisation
    Sub New(inInventoryDisplay As GUIObject.GridView, inInventoryDetail As GUIObject.Label, inInventoryDetailImage As GUIObject.RenderTarget, inInventoryEquip As GUIObject.Button, inContext As OpenGLContext, inInventorySort As GUIObject.DropDown, inInventoryFilter As GUIObject.DropDown)
        '// Set references to GUI objects
        context = inContext
        inventoryContents = New List(Of InventoryItem)
        inventoryDisplay = inInventoryDisplay
        inventoryDetail = inInventoryDetail
        inventoryDetailImage = inInventoryDetailImage
        '// Set delegate functions to the local instance
        For i = 0 To inventoryDisplay.items.Length - 1
            inventoryDisplay.items(i).EventClick = AddressOf SelectItem
        Next
        inInventoryEquip.EventClick = AddressOf EquipItem
        inInventorySort.ListItemBase.EventClick = AddressOf Sort
        inInventoryFilter.ListItemBase.EventClick = AddressOf Filter
        '// Intialise values
        indexLookupFiltered = New List(Of Integer)
        currentFilter = ItemType.NoFilter
    End Sub

    '// Function to count how many of each item there are
    Public Function GetItemCount(itemID As Integer) As Integer
        Dim itemCount As Integer = 0
        For i = 0 To inventoryContents.Count - 1
            If inventoryContents(i).ID = itemID Then
                itemCount += inventoryContents(i).quantity
            End If
        Next
        Return itemCount
    End Function

    '// Read data from a file about each type of item
    Public Sub LoadItemData(folder As String, GLTFProgram As Integer, ByRef audio As AudioManager)
        Dim itemsToLoad As String() = IO.Directory.GetFiles(folder)
        itemEncyclopedia = New List(Of ItemData)
        For i = 0 To itemsToLoad.Length - 1
            itemEncyclopedia.Add(ReadItemData(itemsToLoad(i), GLTFProgram, audio))
        Next
        '// Set default large image to blank
        inventoryDetailImage.model = itemEncyclopedia(0).modelContext
    End Sub

    '// Read data from a file about an item type
    Private Function ReadItemData(filePath As String, GLTFProgram As Integer, ByRef audio As AudioManager) As ItemData
        Dim rawData As String() = IO.File.ReadAllLines(filePath)
        Dim newItem As ItemData
        newItem.name = rawData(0)
        newItem.type = CType(Val(rawData(1)), ItemType)
        newItem.model = New GLTFModel(rawData(2), GLTFProgram, context)
        newItem.modelContext = New Mob(newItem.model, audio)
        newItem.power = CType(Val(rawData(3)), Integer)
        newItem.attackAnimation = rawData(4).Split(","c)
        newItem.description = rawData(5)
        Select Case rawData(6)
            Case "SINGLE"
                newItem.attackType = DamageType.Focused
            Case "SPLASH"
                newItem.attackType = DamageType.Splash
        End Select
        Return newItem
    End Function

    '// Linear search for an item index by name
    Public Function GetItemID(name As String) As Integer
        For i = 0 To itemEncyclopedia.Count - 1
            If itemEncyclopedia(i).name = name Then
                Return i
            End If
        Next
        '// Return -1 if not found
        Return -1
    End Function

    '// Overload functions for adding and removing items by name
    Public Sub AddItem(name As String, quantity As Integer, level As Integer)
        AddItem(GetItemID(name), quantity, level)
    End Sub

    Public Sub RemoveItem(name As String, quantity As Integer)
        RemoveItem(GetItemID(name), quantity)
    End Sub

    '// Subroutine to be called when an inventory grid is clicked on
    Public Sub SelectItem(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        Dim itemIndex As Integer
        Dim newItem As ItemData
        If sender = -1 Then
            '// Clear the selected item (reset to default)
            inventoryDetailImage.model = itemEncyclopedia(0).modelContext
            inventoryDetail.text = ""
            selectedItem.baseData = New ItemData
            selectedItem.specificData = New InventoryItem
        Else
            '// Get item data
            itemIndex = indexLookupFiltered(16 * inventoryDisplay.currentPage + arguments)
            newItem = itemEncyclopedia(inventoryContents(itemIndex).ID)

            '// Load item images and caption
            inventoryDetailImage.model = newItem.modelContext
            inventoryDetail.text = newItem.GetText(inventoryContents(itemIndex).quantity, inventoryContents(itemIndex).level)
            selectedItem.baseData = newItem
            selectedItem.specificData = inventoryContents(itemIndex)
        End If
    End Sub

    '// Subroutine called when equip item button is pressed
    Public Sub EquipItem(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        If Not IsNothing(selectedItem.baseData.model) Then
            Select Case selectedItem.baseData.type
                Case ItemType.NoItem
                    '// Dequip all items
                    GameWorld.character.children(0).model = selectedItem.baseData.model
                    GameWorld.character.children(1).model = selectedItem.baseData.model
                    GameWorld.character.weapon = selectedItem
                    GameWorld.character.shield = selectedItem
                Case ItemType.Weapon
                    '// Equip in primary hand
                    GameWorld.character.children(0).model = selectedItem.baseData.model
                    GameWorld.character.weapon = selectedItem
                Case ItemType.Shield
                    '// Equip shield in secondary hand
                    GameWorld.character.children(1).model = selectedItem.baseData.model
                    GameWorld.character.shield = selectedItem
                Case ItemType.Resource
                    '// Equip resources as though it were a weapon
                    GameWorld.character.children(0).model = selectedItem.baseData.model
                    GameWorld.character.weapon = selectedItem
            End Select
        End If
    End Sub

    '// Function to be called when a sort option is selected
    Public Sub Sort(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        Dim values As Double()
        Dim indices(inventoryContents.Count - 1) As Integer

        '// Initialise mapping
        For i = 0 To indices.Count - 1
            indices(i) = i
        Next

        '// Generate values for each item based on the property to sort
        Select Case CType(sender, SortType)
            Case SortType.Name
                values = GetValuesName(inventoryContents.ToArray())
            Case SortType.Power
                values = GetValuesPower(inventoryContents.ToArray())
            Case SortType.Type
                values = GetValuesType(inventoryContents.ToArray())
        End Select
        '// Sort the pairs of indices and values
        indexLookup = Sorts.MergeSort(values, indices)
        '// Apply a filter as necessary
        Filter(formArray, currentForm, 0, currentFilter + 1)
    End Sub

    '// Generate a list of items to show that meet current selection criteria 
    Public Sub Filter(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        '// Initialise item indices
        indexLookupFiltered.Clear()
        currentFilter = sender - 1
        For i = 0 To indexLookup.Length - 1
            If itemEncyclopedia(inventoryContents(indexLookup(i)).ID).type = currentFilter Or currentFilter = ItemType.NoFilter Then
                '// Add item to be displayed
                indexLookupFiltered.Add(indexLookup(i))
            End If
        Next
        '// Refresh display
        SetPage(inventoryDisplay.currentPage)
    End Sub

    '// Return an array of floating point numbers based on the type of item
    Private Function GetValuesType(items As InventoryItem()) As Double()
        Dim values(items.Length - 1) As Double
        For i = 0 To items.Length - 1
            values(i) = itemEncyclopedia(items(i).ID).type
        Next
        Return values
    End Function

    '// Return an array of floats based on the level of an item
    Private Function GetValuesPower(items As InventoryItem()) As Double()
        Dim values(items.Length - 1) As Double
        For i = 0 To items.Length - 1
            values(i) = items(i).level
            '// Quantity of item is used as a secondary factor
            values(i) += items(i).quantity / 1000
            '// Sort by most powerful first (descending)
            values(i) *= -1
        Next
        Return values
    End Function

    '// Convert names to numeric values to be sorted
    Private Function GetValuesName(items As InventoryItem()) As Double()
        Dim values(items.Length - 1) As Double
        For i = 0 To items.Length - 1
            values(i) = StringToDouble(itemEncyclopedia(items(i).ID).name)
        Next
        Return values
    End Function

    '// Convert a string to a float value for use in sorting
    Private Function StringToDouble(strToConvert As String) As Double
        Dim value As Double = 0
        For i = 0 To strToConvert.Length - 1
            '// Letters at the start take precedence
            value += (Asc(strToConvert(i)) And &B11111) / (26 ^ i)
        Next
        Return value
    End Function

    '// Add a new item to the inventory, or increase quantity if it already exists and can be stacked
    Public Sub AddItem(ID As Integer, quantity As Integer, level As Integer)
        '// Check for a valid item
        If ID > -1 Then
            '// Initialise new item data
            Dim newItemData As New InventoryItem
            Dim alreadyExists As Boolean = False
            newItemData.ID = ID
            newItemData.quantity = quantity
            newItemData.level = level

            If itemEncyclopedia(newItemData.ID).type = ItemType.Resource Then
                '// Try to add the resource to an existign slot
                For i = 0 To inventoryContents.Count - 1
                    If inventoryContents(i).ID = newItemData.ID And Not alreadyExists Then
                        '// Replace existing slot with one with increased quantity
                        newItemData.quantity += inventoryContents(i).quantity
                        inventoryContents.RemoveAt(i)
                        inventoryContents.Insert(i, newItemData)
                        alreadyExists = True
                    End If
                Next
            End If
            '// If it has not been added to an existing slot, add a new item
            If Not alreadyExists Then
                inventoryContents.Add(newItemData)
            End If
            RefreshInventory()
        End If
    End Sub

    '// Ensure the items are refreshed
    Public Sub RefreshInventory()
        Sort({}, 0, 0, 0)
        '// Check for mission completion if the mission is to obtain an item
        missionList.InventoryChange(Me)
    End Sub

    '// Remove a quantity of items from the inventory and delete the slot if it become empty
    Public Sub RemoveItem(ID As Integer, quantity As Integer)
        Dim newInventoryItem As New InventoryItem
        Dim removed As Boolean = False
        Dim i As Integer
        '// Check for valid item
        If ID > -1 Then
            While i < inventoryContents.Count And Not removed
                If inventoryContents(i).ID = ID Then
                    '// Reduce quantity of item
                    newInventoryItem.ID = ID
                    newInventoryItem.quantity = inventoryContents(i).quantity - quantity
                    newInventoryItem.level = inventoryContents(i).level
                    inventoryContents.RemoveAt(i)
                    If newInventoryItem.quantity > 0 Then
                        '// If there are still some left over, replace with reduced quantity
                        inventoryContents.Insert(i, newInventoryItem)
                    End If
                    removed = True
                End If
                i += 1
            End While
            RefreshInventory()
        End If
    End Sub

    '// Update inventory display, showing a specific page
    Public Sub SetPage(page As Integer)
        Dim maxPages As Integer = Math.Max((indexLookupFiltered.Count - 1) \ 16, 0)
        Dim numDisplay As Integer = 16
        Dim itemToDisplay As ItemData

        '// Check for out of range
        If page < 0 Then page = 0
        If page > maxPages Then page = maxPages
        If page = maxPages Then
            numDisplay = indexLookupFiltered.Count Mod 16
            If numDisplay = 0 And indexLookupFiltered.Count > 0 Then numDisplay = 16
        End If

        '// Check to display next or previous arrows
        inventoryDisplay.firstPage = page = 0
        inventoryDisplay.lastPage = page = maxPages

        inventoryDisplay.itemCount = numDisplay
        For i = 0 To numDisplay - 1
            itemToDisplay = itemEncyclopedia(inventoryContents(indexLookupFiltered(page * 16 + i)).ID)
            inventoryDisplay.items(i).caption.text = itemToDisplay.name
            inventoryDisplay.items(i).image.model = itemToDisplay.modelContext
            '// Add additional information
            '// Data displayed depends on the item type
            Select Case itemToDisplay.type
                Case ItemType.NoItem
                    inventoryDisplay.items(i).additionalInfo.text = ""
                Case ItemType.Resource
                    inventoryDisplay.items(i).additionalInfo.text = inventoryContents(indexLookupFiltered(page * 16 + i)).quantity.ToString
                Case ItemType.Shield, ItemType.Weapon
                    inventoryDisplay.items(i).additionalInfo.text = inventoryContents(indexLookupFiltered(page * 16 + i)).level.ToString
            End Select
        Next
    End Sub

    '// Structure declarations

    Public Structure ItemData
        Public name As String
        Public type As ItemType
        Public model As GLTFModel
        Public modelContext As Mob
        Public power As Integer
        Public attackAnimation As String()
        Public description As String
        Public attackType As DamageType
        Public Function GetText(quantity As Integer, level As Integer) As String
            Dim infoText As String = name & "<Newline>" & description & "<Newline>"
            Select Case type
                Case ItemType.Resource
                    infoText &= "Resource<Newline>Quantity " & quantity
                Case ItemType.Shield
                    infoText &= "Shield<Newline>Block Chance " & Math.Round(Player.ShieldChance(level, power) * 100, 1) & "<Percent>"
                Case ItemType.Weapon
                    infoText &= "Weapon<Newline>Attack Power " & Math.Floor(power * (1 + 0.2 * level))
                Case Else
            End Select
            Return infoText
        End Function
    End Structure

    Public Structure InventoryItem
        Public ID As Integer
        Public quantity As Integer
        Public level As Integer
    End Structure

    Public Structure ItemInstance
        Public baseData As ItemData
        Public specificData As InventoryItem
    End Structure

    '// Enum declarations

    Public Enum ItemType
        NoFilter = -1
        NoItem = -1
        Resource = 0
        Weapon = 1
        Shield = 2
    End Enum

    Public Enum SortType
        Name = 0
        Power = 1
        Type = 2
    End Enum

    Public Enum DamageType
        Focused = 0
        Splash = 1
    End Enum
End Class

