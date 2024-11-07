Option Strict On
'// Class for crafting GUI and logic
Public Class Crafting
    Private context As OpenGLContext
    Public craftingDetail As GUIObject.Label
    Public craftingDetailImage As GUIObject.RenderTarget
    Public recipeEncyclopedia As List(Of Recipe)
    Public craftingInventory As Inventory
    Public craftingDisplay As GUIObject.GridView
    Public selectedRecipe As Recipe
    Private craftButton As GUIObject.Button
    Private currentRecipe As Integer

    '// Initialisation of crafting
    Sub New(inCraftingDisplay As GUIObject.GridView, inCraftingDetail As GUIObject.Label, inCraftingDetailImage As GUIObject.RenderTarget, inCraftingCraft As GUIObject.Button, inContext As OpenGLContext, ByRef inCraftingInventory As Inventory)
        context = inContext
        craftingDetail = inCraftingDetail
        craftingDetailImage = inCraftingDetailImage
        craftingInventory = inCraftingInventory
        craftingDisplay = inCraftingDisplay
        craftButton = inCraftingCraft
        recipeEncyclopedia = New List(Of Recipe)

        '// Initialise GUI button press targets
        For i = 0 To inCraftingDisplay.items.Length - 1
            inCraftingDisplay.items(i).EventClick = AddressOf SelectRecipe
        Next
        craftButton.EventClick = AddressOf CraftRecipe
    End Sub

    '// Read crafting recipes from a file
    Public Sub LoadRecipeData(folder As String, GLTFProgram As Integer)
        Dim recipesToLoad As String() = IO.Directory.GetFiles(folder)
        recipeEncyclopedia = New List(Of Recipe)
        For i = 0 To recipesToLoad.Length - 1
            recipeEncyclopedia.Add(ReadRecipeData(recipesToLoad(i), GLTFProgram))
        Next
        '// Initialise selected item to NoItem
        craftingDetailImage.model = craftingInventory.itemEncyclopedia(recipeEncyclopedia(0).output).modelContext
    End Sub

    '// Subroutine called by button to craft an item
    Public Sub CraftRecipe(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        '// Craft item
        If CanCraft(selectedRecipe.input, craftingInventory.inventoryContents.ToArray()) Then
            '// Create new item
            craftingInventory.AddItem(selectedRecipe.output, 1, 1)
            '// Remove ingredients required to craft
            For i = 0 To selectedRecipe.input.Length - 1
                craftingInventory.RemoveItem(selectedRecipe.input(i).itemId, selectedRecipe.input(i).itemQuantity)
            Next
        End If
        '// Update display
        RefreshDetails(currentRecipe)
    End Sub

    '// Check if there are sufficient resources to craft an item
    '// Will check multiple resource types as necessary
    Private Function CanCraft(requirements As RecipeInput(), inventoryContents As Inventory.InventoryItem()) As Boolean
        '// Iterate through each requirement
        For i = 0 To requirements.Length - 1
            If Not ResourceSufficient(inventoryContents, requirements(i)) Then Return False
        Next
        Return True
    End Function

    '// Called by button click when item is selected
    Public Sub SelectRecipe(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        If sender = -1 Then
            '// Reset details
            currentRecipe = -1
        Else
            '// Get recipe
            currentRecipe = 16 * craftingDisplay.currentPage + sender
        End If
        '// Update details pane
        RefreshDetails(currentRecipe)
    End Sub

    '// Update details pane
    Private Sub RefreshDetails(recipeID As Integer)
        Dim newItem As Inventory.ItemData

        currentRecipe = recipeID
        If currentRecipe = -1 Then
            '// Case to clear details pane
            craftingDetailImage.model = craftingInventory.itemEncyclopedia(0).modelContext
            craftingDetail.text = ""
            craftButton.enabled = False
        Else
            '// Select item
            selectedRecipe = recipeEncyclopedia(recipeID)
            newItem = craftingInventory.itemEncyclopedia(selectedRecipe.output)

            '// Display item image and detail text
            craftingDetailImage.model = newItem.modelContext
            craftingDetail.text = newItem.GetText(0, 0)
            For i = 0 To selectedRecipe.input.Length - 1
                craftingDetail.text &= "<Newline><#000000>" & craftingInventory.itemEncyclopedia(selectedRecipe.input(i).itemId).name
                If ResourceSufficient(craftingInventory.inventoryContents.ToArray(), selectedRecipe.input(i)) Then
                    craftingDetail.text &= "<#00FF00>"
                Else
                    craftingDetail.text &= "<#FF0000>"
                End If
                craftingDetail.text &= " x" & selectedRecipe.input(i).itemQuantity
            Next
            craftButton.enabled = CanCraft(selectedRecipe.input, craftingInventory.inventoryContents.ToArray)
        End If
    End Sub

    '// Check if there are enough of one resource to craft an item
    Private Function ResourceSufficient(inventoryContents As Inventory.InventoryItem(), requirement As RecipeInput) As Boolean
        For j = 0 To inventoryContents.Length - 1
            If requirement.itemId = inventoryContents(j).ID And inventoryContents(j).quantity >= requirement.itemQuantity Then
                Return True
            End If
        Next
        Return False
    End Function

    '// Update the grid display to the current page
    Public Sub SetPage(page As Integer)
        Dim maxPages As Integer = Math.Max((recipeEncyclopedia.Count - 1) \ 16, 0)
        Dim numDisplay As Integer = 16
        Dim recipeToDisplay As Recipe
        Dim itemToDisplay As Inventory.ItemData

        '// Clamp page number
        If page < 0 Then page = 0
        If page > maxPages Then page = maxPages
        If page = maxPages Then
            numDisplay = recipeEncyclopedia.Count Mod 16
            If numDisplay = 0 Then numDisplay = 16
        End If

        '// Enable next and previous buttons
        craftingDisplay.firstPage = page = 0
        craftingDisplay.lastPage = page = maxPages

        '// Update grid display
        craftingDisplay.itemCount = numDisplay
        For i = 0 To numDisplay - 1
            recipeToDisplay = recipeEncyclopedia(page * 16 + i)
            itemToDisplay = craftingInventory.itemEncyclopedia(recipeToDisplay.output)
            craftingDisplay.items(i).caption.text = itemToDisplay.name
            craftingDisplay.items(i).image.model = itemToDisplay.modelContext
        Next
    End Sub

    '// Read crafting data from a file and create item images
    Private Function ReadRecipeData(filePath As String, GLTFProgram As Integer) As Recipe
        Dim rawData As String() = IO.File.ReadAllLines(filePath)
        Dim newRecipe As New Recipe
        Dim currentInput As RecipeInput
        Dim inputList As New List(Of RecipeInput)

        newRecipe.output = GetIndexOfItem(craftingInventory.itemEncyclopedia, rawData(0))
        For i = 1 To rawData.Length - 1
            currentInput = New RecipeInput
            currentInput.itemId = GetIndexOfItem(craftingInventory.itemEncyclopedia, rawData(i).Split(","c)(0))
            currentInput.itemQuantity = CInt(rawData(i).Split(","c)(1))
            inputList.Add(currentInput)
        Next
        newRecipe.input = inputList.ToArray()
        Return newRecipe
    End Function

    '// Linear search to get item index
    Private Function GetIndexOfItem(items As List(Of Inventory.ItemData), itemName As String) As Integer
        For i = 0 To items.Count - 1
            If items(i).name.ToLower = itemName.ToLower Then
                Return i
            End If
        Next
        Return 0
    End Function

    '// Structures

    Public Structure Recipe
        Public input As RecipeInput()
        Public output As Integer
    End Structure

    Public Structure RecipeInput
        Public itemId As Integer
        Public itemQuantity As Integer
    End Structure

End Class

