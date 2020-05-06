﻿Imports cv = OpenCvSharp
Public Class Diff_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim lastFrame As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Diff - Color Threshold", 1, 255, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "Stable Gray Color"
        ocvb.label2 = "Unstable Gray Color"
        ocvb.desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If ocvb.frameCount > 0 Then
            ocvb.result1 = lastFrame
            cv.Cv2.Absdiff(gray, lastFrame, ocvb.result2)
            ocvb.result2 = ocvb.result2.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
            ocvb.result1 = ocvb.color.Clone().SetTo(0, ocvb.result2)
        End If
        lastFrame = gray.Clone()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Diff_UnstableDepthAndColor : Implements IDisposable
    Dim diff As Diff_Basics
    Dim depth As Depth_Stable
    Dim lastFrames() As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        diff = New Diff_Basics(ocvb, "Diff_UnstableDepthAndColor")
        diff.sliders.TrackBar1.Value = 20 ' this is color threshold - low means detecting more motion.

        depth = New Depth_Stable(ocvb, "Diff_UnstableDepthAndColor")

        ocvb.desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        diff.Run(ocvb)
        Dim unstableColor = ocvb.result2.Clone()
        depth.Run(ocvb)
        If ocvb.result2.Channels = 1 Then
            Dim unstableDepth As New cv.Mat
            Dim mask As New cv.Mat
            cv.Cv2.BitwiseNot(ocvb.result2, unstableDepth)
            If unstableColor.Channels = 3 Then unstableColor = unstableColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If ocvb.result1.Channels = 3 Then ocvb.result1 = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            cv.Cv2.BitwiseOr(unstableColor, unstableDepth, mask)
            ocvb.result1 = ocvb.color.Clone()
            ocvb.result1.SetTo(0, mask)
            ocvb.label1 = "Stable depth and color"
            ocvb.label2 = "Stable (non-zero) Depth"
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        diff.Dispose()
        depth.Dispose()
    End Sub
End Class