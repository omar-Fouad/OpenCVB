﻿Imports cv = OpenCvSharp
Imports System.IO
#If USE_NUMPY Then
Imports Numpy
Imports py = Python.Runtime
#End If
Public Class OptionsDialog
    Dim numPyEnabled As Boolean = False

    Public cameraIndex As integer ' an index into the cameraRadioButton array.

    Public cameraDeviceCount(VB_Classes.ActiveTask.algParms.D455) As integer
    Public cameraRadioButton(VB_Classes.ActiveTask.algParms.D455) As RadioButton
    Public cameraTotalCount As Integer = 0

    Public Const lowRes = 0
    Public Const medRes = 1
    Public Const highRes = 2

    Public resolutionResizeFactor As Single = 1
    Public resolutionName As String = "High"

    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        SaveSetting("OpenCVB", "resolutionXY", "resolutionXY", OpenCVB.resolutionSetting)
        SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", cameraIndex)

        SaveSetting("OpenCVB", "ShowLabels", "ShowLabels", ShowLabels.Checked)

        SaveSetting("OpenCVB", "TestAllDuration", "TestAllDuration", TestAllDuration.Value)
        SaveSetting("OpenCVB", "SnapToGrid", "SnapToGrid", SnapToGrid.Checked)
        SaveSetting("OpenCVB", "PythonExe", "PythonExe", PythonExeName.Text)
        SaveSetting("OpenCVB", "ShowConsoleLog", "ShowConsoleLog", ShowConsoleLog.Checked)
        SaveSetting("OpenCVB", "EnableNumPy", "EnableNumPy", EnableNumPy.Checked)
        SaveSetting("OpenCVB", "FontName", "FontName", fontInfo.Font.Name)
        SaveSetting("OpenCVB", "FontSize", "FontSize", fontInfo.Font.Size)

        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        cameraIndex = sender.tag
    End Sub
    Public Sub enableCameras()
        For i = 0 To cameraRadioButton.Count - 1
            If cameraDeviceCount(i) > 0 Then cameraRadioButton(i).Enabled = True
        Next
    End Sub
    Public Sub saveResolution()
        Select Case OpenCVB.resolutionSetting
            Case 0
                LowResolution.Checked = True
                OpenCVB.resolutionXY = New cv.Size(320, 180)
                resolutionResizeFactor = 0.25
                resolutionName = "Low"
            Case 1
                mediumResolution.Checked = True
                OpenCVB.resolutionXY = New cv.Size(640, 360)
                resolutionResizeFactor = 0.5
                resolutionName = "Medium"
            Case 2
                HighResolution.Checked = True
                OpenCVB.resolutionXY = New cv.Size(1280, 720)
                resolutionResizeFactor = 1
                resolutionName = "High"
        End Select
    End Sub
    Public Sub TestEnableNumPy()
#If USE_NUMPY Then
               If EnableNumPy.Checked Then
            If numPyEnabled = False Then
                numPyEnabled = True
                ' This allows the VB_Classes to use NumPy and then reuse it.  OpenCVB.exe does not use NumPy but must do this to allow the child threads to use NumPy
                ' see https://github.com/SciSharp/Numpy.NET
                np.arange(1)
                py.PythonEngine.BeginAllowThreads()
            End If
        End If
#Else
        numPyEnabled = False
#End If
    End Sub
    Public Sub OptionsDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        For i = 0 To cameraRadioButton.Count - 1
            cameraRadioButton(i) = New RadioButton
            CameraGroup.Controls.Add(cameraRadioButton(i))
            cameraRadioButton(i).Visible = True
            If cameraDeviceCount(i) = 0 Then cameraRadioButton(i).Enabled = False
            cameraRadioButton(i).AutoSize = True
            cameraRadioButton(i).BringToFront()
            cameraRadioButton(i).Tag = i ' this will manage the public type for the camera - see VB_Classes.vb.
            cameraRadioButton(i).Location = New Point(16, (i + 1) * 20)
            cameraRadioButton(i).Text = Choose(i + 1, "Microsoft Kinect for Azure Camera", "Intel RealSense T265", "StereoLabs ZED 2 camera",
                                               "MyntEyeD 1000 camera", "Intel RealSense D435i", "Intel RealSense L515", "Intel RealSense D455")
            AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
        Next

        OpenCVB.resolutionSetting = GetSetting("OpenCVB", "resolutionXY", "resolutionXY", medRes)
        saveResolution()

        cameraIndex = GetSetting("OpenCVB", "CameraIndex", "CameraIndex", VB_Classes.ActiveTask.algParms.D435i)
        cameraRadioButton(cameraIndex).Checked = True

        ShowLabels.Checked = GetSetting("OpenCVB", "ShowLabels", "ShowLabels", False)

        TestAllDuration.Value = If(GetSetting("OpenCVB", "TestAllDuration", "TestAllDuration", 2) < 2, 2,
                                   GetSetting("OpenCVB", "TestAllDuration", "TestAllDuration", 2))
        SnapToGrid.Checked = GetSetting("OpenCVB", "SnapToGrid", "SnapToGrid", True)
        ShowConsoleLog.Checked = GetSetting("OpenCVB", "ShowConsoleLog", "ShowConsoleLog", False)
        EnableNumPy.Checked = False ' GetSetting("OpenCVB", "EnableNumPy", "EnableNumPy", False)

        Dim defaultSize = GetSetting("OpenCVB", "FontSize", "FontSize", 8)
        Dim DefaultFont = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        fontInfo.Font = New Drawing.Font(DefaultFont, defaultSize)
        fontInfo.Text = DefaultFont + " with size = " + CStr(defaultSize)

        Dim selectionName = GetSetting("OpenCVB", "PythonExe", "PythonExe", "")
        Dim selectionInfo As FileInfo = Nothing
        If selectionName <> "" Then
            selectionInfo = New FileInfo(selectionName)
            PythonExeName.Text = selectionInfo.FullName
        End If
        TestEnableNumPy()
    End Sub
    Private Sub OptionsDialog_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Me.Hide()
        If e.KeyCode = Keys.Escape Then Cancel_Button_Click(sender, e)
    End Sub
    Private Sub SelectPythonFile_Click(sender As Object, e As EventArgs) Handles SelectPythonFile.Click
        Dim pythonInfo As FileInfo
        OpenFileDialog1.FileName = "Python.exe"
        If PythonExeName.Text <> "" Then
            pythonInfo = New FileInfo(PythonExeName.Text)
            OpenFileDialog1.InitialDirectory = pythonInfo.DirectoryName
        Else
            OpenFileDialog1.InitialDirectory = "C:\\"
        End If
        OpenFileDialog1.Filter = "*.exe (*.exe) | *.exe"
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            pythonInfo = New FileInfo(OpenFileDialog1.FileName)
            PythonExeName.Text = pythonInfo.FullName
        End If
    End Sub
    Private Sub Cancel_Button_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
        OptionsDialog_Load(sender, e) ' restore the settings to what they were on entry...
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Hide()
    End Sub

    Private Sub TestAllDuration_ValueChanged(sender As Object, e As EventArgs) Handles TestAllDuration.ValueChanged
        If TestAllDuration.Value < 1 Then TestAllDuration.Value = 1
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        FontDialog1.ShowColor = False
        FontDialog1.ShowApply = False
        FontDialog1.ShowEffects = False
        FontDialog1.ShowHelp = True

        FontDialog1.MaxSize = 40
        FontDialog1.MinSize = 5

        If FontDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
            fontInfo.Font = FontDialog1.Font
            fontInfo.Text = FontDialog1.Font.Name + " with size = " + CStr(fontInfo.Font.Size)
        End If
    End Sub
    Private Sub HighResolution_CheckedChanged(sender As Object, e As EventArgs) Handles HighResolution.CheckedChanged
        OpenCVB.resolutionSetting = highRes
    End Sub
    Private Sub mediumResolution_CheckedChanged(sender As Object, e As EventArgs) Handles mediumResolution.CheckedChanged
        OpenCVB.resolutionSetting = medRes
    End Sub
    Private Sub LowResolution_CheckedChanged(sender As Object, e As EventArgs) Handles LowResolution.CheckedChanged
        OpenCVB.resolutionSetting = lowRes
    End Sub
End Class
