Imports cv = OpenCvSharp
Public Class Reduction_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reduction factor", 0, 12, 6)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use Reduction"
        check.Box(0).Checked = True

        ocvb.desc = "Reduction: a simpler way to KMeans by removing low-order bits"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If check.Box(0).Checked Then
            Dim power = Choose(sliders.trackbar(0).Value + 1, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096) - 1
            Dim maskval = 256 - power
            If src.Type = cv.MatType.CV_32S Then maskval = Integer.MaxValue - power
            If src.Type = cv.MatType.CV_8U Or src.Type = cv.MatType.CV_8UC3 And maskval < 2 Then
                Console.WriteLine("Reduction_Basics: the limit of the reduction factor for 8-bit images is 7 or fewer and it is set to 8!")
            End If
            Dim tmp = New cv.Mat(src.Size, src.Type).SetTo(cv.Scalar.All(maskval))
            cv.Cv2.BitwiseAnd(src, tmp, dst1)
            label1 = "Reduced color image after zero'ing bit(s) 0x" + Hex(power)
        Else
            dst1 = src
            label1 = "No reduction requested"
        End If
    End Sub
End Class






Public Class Reduction_Simple
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Simple reduction factor", 1, 4000, 64)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use Simple Reduction"
        check.Box(0).Checked = True

        ocvb.desc = "Reduction: a simple way to get KMeans"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If check.Box(0).Checked Then
            dst1 = src / sliders.trackbar(0).Value ' can be any mat type...
            dst1 *= sliders.trackbar(0).Value
            label1 = "Reduced image - factor = " + CStr(sliders.trackbar(0).Value)
        Else
            dst1 = src
            label1 = "No reduction requested"
        End If
    End Sub
End Class







Public Class Reduction_Edges
    Inherits VBparent
    Dim edges As Edges_Laplacian
    Dim reduction As Reduction_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        edges = New Edges_Laplacian(ocvb)
        reduction = New Reduction_Basics(ocvb)
        ocvb.desc = "Get the edges after reducing the image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1.Clone

        Static reductionCheck = findCheckBox("Use Reduction")
        label1 = If(reductionCheck.checked, "Reduced image", "Original image")
        label2 = If(reductionCheck.checked, "Laplacian edges of reduced image", "Laplacian edges of original image")
        edges.src = dst1
        edges.Run(ocvb)
        dst2 = edges.dst1
    End Sub
End Class




Public Class Reduction_Floodfill
    Inherits VBparent
    Public flood As FloodFill_Basics
    Public reduction As Reduction_Simple
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        flood = New FloodFill_Basics(ocvb)
        reduction = New Reduction_Simple(ocvb)
        ocvb.desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)

        flood.src = reduction.dst1
        flood.Run(ocvb)

        dst1 = flood.dst2
        label1 = flood.label2
    End Sub
End Class






Public Class Reduction_KNN_Color
    Inherits VBparent
    Public reduction As Reduction_Floodfill
    Public pTrack As Kalman_PointTracker
    Dim highlight As Highlight_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        pTrack = New Kalman_PointTracker(ocvb)
        reduction = New Reduction_Floodfill(ocvb)
        If standalone Then highlight = New Highlight_Basics(ocvb)

        label2 = "Original floodfill color selections"
        ocvb.desc = "Use KNN with color reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        reduction.Run(ocvb)
        dst2 = reduction.dst1

        pTrack.queryPoints = New List(Of cv.Point2f)(reduction.flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(reduction.flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(reduction.flood.masks)
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        If standalone Then
            highlight.viewObjects = pTrack.viewObjects
            highlight.src = dst1
            highlight.Run(ocvb)
            dst1 = highlight.dst1
        End If

        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        label1 = "There were " + CStr(pTrack.viewObjects.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class







Public Class Reduction_KNN_ColorAndDepth
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Dim depth As Depth_Edges
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        depth = New Depth_Edges(ocvb)
        reduction = New Reduction_KNN_Color(ocvb)
        label1 = "Detecting objects using only color coherence"
        label2 = "Detecting objects with color and depth coherence"
        ocvb.desc = "Reduction_KNN finds objects with depth.  This algorithm uses only color on the remaining objects."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1

        depth.Run(ocvb)
        dst2 = depth.dst1
    End Sub
End Class






Public Class Reduction_Depth
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_Basics(ocvb)
        colorizer = New Depth_Colorizer_CPP(ocvb)
        ocvb.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Type = cv.MatType.CV_32S Then
            reduction.src = src
        Else
            src = getDepth32f(ocvb)
            src.ConvertTo(reduction.src, cv.MatType.CV_32S)
        End If
        reduction.Run(ocvb)
        reduction.dst1.ConvertTo(dst1, cv.MatType.CV_32F)
        colorizer.src = dst1
        colorizer.Run(ocvb)
        dst2 = colorizer.dst1
        label1 = reduction.label1
    End Sub
End Class





Public Class Reduction_PointCloud
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Public newPointCloud As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_Basics(ocvb)
        ocvb.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim split() = ocvb.pointCloud.Split()
        split(2) *= 1000 ' convert to mm's
        split(2).ConvertTo(reduction.src, cv.MatType.CV_32S)
        reduction.Run(ocvb)
        reduction.dst1.ConvertTo(dst2, cv.MatType.CV_32F)
        dst1 = dst2.Resize(ocvb.pointCloud.Size)
        split(2) = dst1 / 1000
        cv.Cv2.Merge(split, newPointCloud)
        dst1 = dst1.ConvertScaleAbs(255).CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size)
    End Sub
End Class