﻿Imports System.Windows.Forms

Public Class OptionsKeyboardInput
    Public inputText As New List(Of String)
    Dim keyboardLastInput As String
    Public Sub Setup(caller As String)
        Me.Text = caller + " CheckBox Options"
        Me.Show() ' only the first one   gets to be visible...
    End Sub
    Private Sub TextBox1_KeyUp(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyUp
        HoldKeyTimer.Enabled = False
        inputText.Add(e.KeyCode.ToString)
    End Sub
    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        keyboardLastInput = e.KeyCode.ToString
        HoldKeyTimer.Enabled = True
    End Sub
    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles keyboardCheckbox.CheckedChanged
        My.Computer.Keyboard.SendKeys(vbTab, True) ' shift focus to the textbox...
    End Sub
    Private Sub OptionsKeyboardInput_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(midFormX + radioOffset.X, applocation.Top + applocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
        If radioOffset.X > offsetMax Then radioOffset.X = 0
        If radioOffset.Y > offsetMax Then radioOffset.Y = 0
    End Sub
    Private Sub TextBox1_LostFocus(sender As Object, e As EventArgs) Handles TextBox1.LostFocus
        keyboardCheckbox.Checked = False
    End Sub
    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        TextBox1.Text = ""
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles HoldKeyTimer.Tick
        inputText.Add(keyboardLastInput) ' press and hold means send this key again...
    End Sub
End Class