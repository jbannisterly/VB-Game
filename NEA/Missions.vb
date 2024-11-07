Option Strict On
'// Class to manage missions and the display of missions
Public Class Missions
    '// Variable declaration
    Public displayMission As GUIObject.TaskList
    Public displayTask As GUIObject.TaskList
    Public missionEncyclopedia As List(Of MissionGeneral)
    Public currentMissions As List(Of MissionInstance)
    Public enemies As EnemyManager
    Public inventoryContents As Inventory

    '// Set references to form controls to be displayed
    Sub New(ByRef inDisplayMission As GUIObject.TaskList, ByRef inDisplayTask As GUIObject.TaskList)
        currentMissions = New List(Of MissionInstance)

        displayMission = inDisplayMission
        displayTask = inDisplayTask

        '// Set the butto click targets to the subroutines in this class instance
        displayMission.EventClick = AddressOf SelectMission
        displayTask.EventClick = AddressOf Menu.ButtonTest
    End Sub

    '// Linear search for mission index by name
    Public Function GetMissionIndex(missionName As String) As Integer
        For i = 0 To currentMissions.Count - 1
            If missionEncyclopedia(currentMissions(i).missionID).identifier = missionName Then
                Return i
            End If
        Next
        Return -1
    End Function

    '// Add a mission to the list
    Public Sub SetMission(missionID As Integer)
        Dim newMission As New MissionInstance()
        '// Initialise progress to 0
        newMission.currentTask = 0
        newMission.currentTaskProgress = 0
        newMission.missionID = missionID
        currentMissions.Add(newMission)
    End Sub

    '// To be called when the player progresses to the next task
    Public Sub AdvanceMission(missionName As String)
        Dim index As Integer = GetMissionIndex(missionName)
        '// Unlock next task
        currentMissions(index).currentTask += 1
        '// Reset task progress
        currentMissions(index).currentTaskProgress = 0
        '// Check if the player has completed the new task already
        '// The user may have already obtained the required items
        InventoryChange(inventoryContents)
    End Sub

    '// Called when an enemy is killed
    Public Sub KilledEnemy(mobType As Integer)
        Dim currentTask As TaskGeneral
        For i = 0 To currentMissions.Count - 1
            '// Check if the mission is in progress
            If Not MissionComplete(currentMissions(i), missionEncyclopedia) Then
                currentTask = missionEncyclopedia(currentMissions(i).missionID).tasks(currentMissions(i).currentTask)
                '// Check if the mob type is the type of mob to target
                If currentTask.typeOfTask = TaskType.KILL And currentTask.taskTarget = mobType Then
                    '// Check if task is in progress
                    If currentMissions(i).currentTaskProgress < currentTask.taskQuantity Then
                        currentMissions(i).currentTaskProgress += 1
                    End If
                End If
            End If
        Next
    End Sub

    '// Returns a boolean indicating whether a mission has been completed
    Private Function MissionComplete(ByRef mission As MissionInstance, ByRef missionReference As List(Of MissionGeneral)) As Boolean
        Return missionReference(mission.missionID).tasks.Length <= mission.currentTask
    End Function

    '// Called when the inventory items changes
    '// Recalculate the progress of missions that require items be collected
    Public Sub InventoryChange(inventoryContents As Inventory)
        Dim currentTask As TaskGeneral
        For i = 0 To currentMissions.Count - 1
            If Not MissionComplete(currentMissions(i), missionEncyclopedia) Then
                currentTask = missionEncyclopedia(currentMissions(i).missionID).tasks(currentMissions(i).currentTask)
                If currentTask.typeOfTask = TaskType.OBTAIN Then
                    currentMissions(i).currentTaskProgress = inventoryContents.GetItemCount(currentTask.taskTarget)
                End If
            End If
        Next
    End Sub

    '// Load all missions from a file to the mission encyclopedia
    Public Sub LoadMissionData(folder As String)
        Dim missionsToLoad As String() = IO.Directory.GetFiles(folder)
        missionEncyclopedia = New List(Of MissionGeneral)
        For i = 0 To missionsToLoad.Length - 1
            missionEncyclopedia.Add(New MissionGeneral(IO.File.ReadAllLines(missionsToLoad(i)), enemies, inventoryContents))
        Next
    End Sub

    '// Refresh mission list to display all missions and show updated progress
    Public Sub LoadAllMissions()
        displayMission.ClearTask()
        For i = 0 To currentMissions.Count - 1
            displayMission.SetTask(missionEncyclopedia(currentMissions(i).missionID).displayName, currentMissions(i).currentTask, missionEncyclopedia(currentMissions(i).missionID).tasks.Length, True)
        Next
    End Sub

    '// Check if the current task has been completed
    Public Function TaskComplete(missionIndex As Integer) As Boolean
        Dim selectedMission As MissionInstance = currentMissions(missionIndex)
        If MissionComplete(selectedMission, missionEncyclopedia) Then
            '// If a misison is completed, all tasks are completed
            Return True
        End If
        '// Check progress of current task
        Return selectedMission.currentTaskProgress >= missionEncyclopedia(selectedMission.missionID).tasks(selectedMission.currentTask).taskQuantity
    End Function

    '// Reset task progress on respawn
    Public Sub Respawn()
        For i = 0 To currentMissions.Count - 1
            currentMissions(i).currentTaskProgress = 0
        Next
        InventoryChange(inventoryContents)
    End Sub

    '// Display task list when a mission is selected
    Public Sub SelectMission(ByRef formArray As GUIObject.Form(), ByRef currentForm As Integer, ByRef arguments As Integer, ByRef sender As Integer)
        Dim selectedMission As MissionInstance
        Dim selectedMissionTasks As TaskGeneral()
        displayTask.ClearTask()
        '// -1 is used to clear the mission list
        If sender > -1 Then
            '// Select mission and tasks
            selectedMission = currentMissions(sender)
            selectedMissionTasks = missionEncyclopedia(selectedMission.missionID).tasks
            '// Display completed tasks
            For i = 0 To selectedMission.currentTask - 1
                displayTask.SetTask(selectedMissionTasks(i).name, 1, 1, False)
            Next
            '// Display current task
            If selectedMission.currentTask < selectedMissionTasks.Length Then
                displayTask.SetTask(selectedMissionTasks(selectedMission.currentTask).name, selectedMission.currentTaskProgress, selectedMissionTasks(selectedMission.currentTask).taskQuantity, True)
            End If
            '// Display future tasks as unknown tasks
            For i = selectedMission.currentTask + 1 To selectedMissionTasks.Length - 1
                displayTask.SetTask("Unknown task", 0, 1, False)
            Next
        End If
        InventoryChange(inventoryContents)
    End Sub

    '// Structure declarations

    '// Represents an instance of a mission, with the current progress
    Public Class MissionInstance
        Public missionID As Integer
        Public currentTask As Integer
        Public currentTaskProgress As Integer
    End Class

    '// Contains entire mission data with array of tasks to complete
    Public Structure MissionGeneral
        Public identifier As String
        Public displayName As String
        Public tasks As TaskGeneral()
        '// Load tasks data from array
        Sub New(missionString As String(), ByRef enemies As EnemyManager, ByRef inventoryContents As Inventory)
            identifier = missionString(0)
            displayName = missionString(1)
            ReDim tasks(missionString.Length - 3)
            For i = 0 To tasks.Length - 1
                tasks(i) = New TaskGeneral(missionString(i + 2), enemies, inventoryContents)
            Next
        End Sub
    End Structure

    '// Contains specific requirements for each task
    Public Structure TaskGeneral
        Public name As String
        Public typeOfTask As TaskType
        Public taskQuantity As Integer
        Public taskTarget As Integer
        Sub New(taskString As String, ByRef enemies As EnemyManager, ByRef inventoryContents As Inventory)
            '// Data is of the following form
            '// name : typeOfTask taskQuantity taskTarget
            '// e.g. Kill the lake monsters:KILL 4 Orthoceras
            Dim taskSplit As String() = taskString.Split(":"c)
            Dim taskCode As String() = taskSplit(1).Split(" "c)
            name = taskSplit(0)
            Select Case taskCode(0)
                Case "OBTAIN"
                    typeOfTask = TaskType.OBTAIN
                    taskTarget = inventoryContents.GetItemID(taskCode(2))
                Case "KILL"
                    typeOfTask = TaskType.KILL
                    taskTarget = enemies.GetEnemyIndexByName(taskCode(2))
            End Select
            taskQuantity = CInt(taskCode(1))
        End Sub
    End Structure

    '// Enum declaration

    Public Enum TaskType
        OBTAIN = 0
        KILL = 1
    End Enum

End Class

