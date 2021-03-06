﻿Imports cv = OpenCvSharp
Public Class Highlight_Basics
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Public highlightPoint As New cv.Point
    Dim highlightRect As New cv.Rect
    Dim preKalmanRect As New cv.Rect
    Dim highlightMask As New cv.Mat
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalIntInverted)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        If standalone Then reduction = New Reduction_KNN_Color(ocvb)
        ocvb.desc = "Highlight the rectangle and centroid nearest the mouse click"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then
            reduction.src = src
            reduction.Run(ocvb)
            viewObjects = reduction.pTrack.viewObjects
            src = reduction.dst1
        End If

        dst1 = src
        If ocvb.mouseClickFlag Then
            highlightPoint = ocvb.mouseClickPoint
            ocvb.mouseClickFlag = False ' absorb the mouse click here only
        End If
        If highlightPoint <> New cv.Point And viewObjects.Count > 0 Then
            Dim index = findNearestPoint(highlightPoint, viewObjects)
            highlightPoint = viewObjects.ElementAt(index).Value.centroid
            highlightRect = viewObjects.ElementAt(index).Value.rectView
            highlightMask = New cv.Mat
            highlightMask = viewObjects.ElementAt(index).Value.mask
            preKalmanRect = viewObjects.ElementAt(index).Value.preKalmanRect

            dst1.Circle(highlightPoint, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Rectangle(highlightRect, cv.Scalar.Red, 2)
            Dim rect = New cv.Rect(0, 0, highlightMask.Width, highlightMask.Height)
            ocvb.color.CopyTo(dst2)
            dst2(preKalmanRect).SetTo(cv.Scalar.Yellow, highlightMask)
            label2 = "Highlighting the selected region."
        End If
    End Sub
End Class