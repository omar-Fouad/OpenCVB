﻿Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Drawing

Public Class OptionsSliders
    Public DontShow As Boolean = False
    Public trackbar() As TrackBar
    Public sLabels() As Label
    Public countLabel() As Label
    Dim defaultHeight = 260
    Dim defaultWidth = 630
    Public Sub Setup(ocvb As VBocvb, caller As String, Optional count As Integer = 4)
        ReDim trackbar(count - 1)
        ReDim sLabels(count - 1)
        ReDim countLabel(count - 1)
        Me.Text = caller + " Slider Options"
        Dim yIncr = 100
        For i = 0 To trackbar.Count - 1
            FlowLayoutPanel1.FlowDirection = FlowDirection.LeftToRight
            sLabels(i) = New Label
            sLabels(i).AutoSize = False
            sLabels(i).Width = 100
            sLabels(i).Height = 50
            FlowLayoutPanel1.Controls.Add(sLabels(i))
            trackbar(i) = New TrackBar
            trackbar(i).Width = 440
            trackbar(i).Tag = i
            trackbar(i).Visible = False
            AddHandler trackbar(i).ValueChanged, AddressOf TrackBar_ValueChanged
            FlowLayoutPanel1.Controls.Add(trackbar(i))
            countLabel(i) = New Label
            countLabel(i).AutoSize = False
            countLabel(i).Width = 100
            countLabel(i).Height = 50
            FlowLayoutPanel1.Controls.Add(countLabel(i))
            FlowLayoutPanel1.SetFlowBreak(countLabel(i), True)
        Next
        If count > 4 Then
            defaultHeight = count * 58 ' add space for the additional unexpected trackbars.
            FlowLayoutPanel1.Height = defaultHeight - 30
        End If
        If lookupAlgorithm(caller) = 1 Then Me.Show() ' only the first one gets to be visible...
    End Sub
    Public Sub setupTrackBar(index As Integer, label As String, min As Integer, max As Integer, value As Integer)
        sLabels(index).Text = label
        trackbar(index).Minimum = min
        trackbar(index).Maximum = max
        trackbar(index).Value = value
        trackbar(index).Visible = True
        sLabels(index).Visible = True
        countLabel(index).Text = CStr(value)
        countLabel(index).Visible = True
    End Sub
    Private Sub TrackBar_ValueChanged(sender As Object, e As EventArgs)
        countLabel(sender.tag).Text = CStr(trackbar(sender.tag).Value)
    End Sub
    Private Function lookupAlgorithm(caller As String) As Integer
        For i = 0 To callerNames.Length - 1
            If callerNames(i) = caller Then
                callerSliderCounts(i) += 1
                Return callerSliderCounts(i)
            End If
        Next
        Return 0
    End Function
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
    Private Sub OptionsSliders_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = defaultWidth
        Me.Height = defaultHeight
        Me.SetDesktopLocation(optionLocation.X, optionLocation.Y)
    End Sub
End Class