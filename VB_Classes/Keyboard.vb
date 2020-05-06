﻿
Imports cv = OpenCvSharp
Public Class Keyboard_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        ocvb.label1 = "Type in text to add to image"
        ocvb.desc = "Test the keyboard interface available to all algorithms"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static input As String
        If ocvb.parms.keyboardInput <> "" Then
            input = ocvb.parms.keyboardInput
        End If
        If input = "" Then
            ocvb.putText(New ActiveClass.TrueType("Any text entered will appear here." + input, 10, 50, RESULT1))
        Else
            ocvb.putText(New ActiveClass.TrueType("The last key that was hit was: " + input, 10, 50, RESULT1))
        End If
        ocvb.parms.keyInputAccepted = True
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class