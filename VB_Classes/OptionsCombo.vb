﻿Imports cv = OpenCvSharp
Public Class OptionsCombo
    Public Sub Setup(ocvb As VBocvb, caller As String, label As String, comboList As List(Of String))
        Me.Text = caller + " ComboBox Options"
        Me.Show()
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
        Me.Show()
    End Sub
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property

    Private Sub OptionsCombo_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(optionLocation.X, optionLocation.Y)
    End Sub
End Class
