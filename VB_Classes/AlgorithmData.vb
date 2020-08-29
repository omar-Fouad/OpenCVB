﻿Imports cv = OpenCvSharp

Public Class AlgorithmData
    ' all the items here are used to communicate to/from the host user interface.  Other variables common to all algorithms should be ocvbClass.vb
    Public color As cv.Mat
    Public RGBDepth As cv.Mat
    Public result As New cv.Mat
    Public pointCloud As cv.Mat
    Public depth16 As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat

    Public desc As String
    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public frameCount As Int32 = 0
    Public label1 As String
    Public label2 As String
    Public quadrantIndex As Integer = 0
    Public parms As ActiveClass.algorithmParameters

    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.

    Public PythonExe As String
    Public PythonFileName As String
    Public TTtextData As List(Of TTtext)

    Public algorithmIndex As Integer
    Public parentRoot As String
    Public parentAlgorithm As String
    Public callTrace As New List(Of String)
    Public Sub New(parms As ActiveClass.algorithmParameters, width As Integer, height As Integer)
        color = New cv.Mat(height, width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result = New cv.Mat(height, width * 2, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        TTtextData = New List(Of TTtext)
    End Sub
    Public Sub trueText(tt As TTtext)
        TTtextData.Add(tt)
    End Sub
End Class
