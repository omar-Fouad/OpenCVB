Imports cv = OpenCvSharp
Public Class Diff_Basics
    Inherits VBparent
    Dim lastFrame As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Diff - Color Threshold", 1, 255, 5)
        label1 = "Stable Gray Color"
        label2 = "Unstable Color mask"
        ocvb.desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If ocvb.frameCount > 0 Then
            dst1 = lastFrame
            cv.Cv2.Absdiff(gray, lastFrame, dst2)
            dst2 = dst2.Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
            dst1 = src.Clone().SetTo(0, dst2)
        End If
        lastFrame = gray.Clone()
    End Sub
End Class




Public Class Diff_UnstableDepthAndColor
    Inherits VBparent
    Public diff As Diff_Basics
    Public depth As Depth_Stable
    Dim lastFrames() As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        diff = New Diff_Basics(ocvb)
        diff.sliders.trackbar(0).Value = 20 ' this is color threshold - low means detecting more motion.

        depth = New Depth_Stable(ocvb)

        label1 = "Stable depth and color"
        ocvb.desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        diff.src = src
        diff.Run(ocvb)
        Dim unstableColor = diff.dst2.Clone()
        depth.src = ocvb.RGBDepth
        depth.Run(ocvb)
        Dim unstableDepth As New cv.Mat
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(depth.dst2, unstableDepth)
        If unstableColor.Channels = 3 Then unstableColor = unstableColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseOr(unstableColor, unstableDepth, mask)
        dst1 = src.Clone()
        dst1.SetTo(0, mask)
        label2 = "Unstable depth/color mask"
        dst2 = mask
    End Sub
End Class
