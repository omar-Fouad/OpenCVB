Imports cv = OpenCvSharp
Imports System.Threading
Public Class FloodFill_Basics
    Inherits VBparent
    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)

    Public initialMask As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public minFloodSize As Integer
    Public Class CompareMaskSize : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "FloodFill Minimum Size", 1, 5000, 2500)
        sliders.setupTrackBar(1, "FloodFill LoDiff", 1, 255, 25)
        sliders.setupTrackBar(2, "FloodFill HiDiff", 1, 255, 25)
        sliders.setupTrackBar(3, "Step Size", 1, src.Cols / 2, 10)

        label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        minFloodSize = sliders.trackbar(0).Value
        Dim loDiff = cv.Scalar.All(sliders.trackbar(1).Value)
        Dim hiDiff = cv.Scalar.All(sliders.trackbar(2).Value)
        Dim stepSize = sliders.trackbar(3).Value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src
        Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1)
        Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)
        initialMask = src.EmptyClone().SetTo(0)

        masks.Clear()
        maskSizes.Clear()
        rects.Clear()
        centroids.Clear()

        maskPlus.SetTo(0)
        Dim ignoreMasks = initialMask.Clone()

        Dim gray = src.Clone()
        For y = 0 To gray.Height - 1 Step stepSize
            For x = 0 To gray.Width - 1 Step stepSize
                If gray.Get(Of Byte)(y, x) > 0 Then
                    Dim rect As New cv.Rect
                    Dim count = cv.Cv2.FloodFill(gray, maskPlus, New cv.Point(x, y), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
                    If count > minFloodSize Then
                        masks.Add(maskPlus(maskRect).Clone().SetTo(0, ignoreMasks))
                        masks(masks.Count - 1).SetTo(0, initialMask) ' The initial mask is what should not be part of any mask.
                        maskSizes.Add(rect.Width * rect.Height, masks.Count - 1)
                        rects.Add(rect)
                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
                        centroids.Add(centroid)
                    End If
                    ' Mask off any object that is too small or previously identified
                    cv.Cv2.BitwiseOr(ignoreMasks, maskPlus(maskRect), ignoreMasks)
                End If
            Next
        Next

        dst2.SetTo(0)
        For i = 0 To masks.Count - 1
            Dim maskIndex = maskSizes.ElementAt(i).Value
            dst2.SetTo(ocvb.scalarColors(i Mod 255), masks(maskIndex))
        Next
        label2 = CStr(masks.Count) + " regions > " + CStr(minFloodSize) + " pixels"
    End Sub
End Class






Public Class FloodFill_8bit
    Inherits VBparent
    Public basics As FloodFill_Basics
    Public palette As Palette_Basics
    Public allRegionMask As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        palette = New Palette_Basics(ocvb)
        palette.Run(ocvb)

        basics = New FloodFill_Basics(ocvb)
        ocvb.desc = "Create a floodfill image that is only 8-bit for use with a palette"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        basics.src = src
        basics.Run(ocvb)

        dst2.SetTo(0)
        For i = 0 To basics.masks.Count - 1
            Dim maskIndex = basics.maskSizes.ElementAt(i).Value
            dst2.SetTo(cv.Scalar.All((i + 1) Mod 255), basics.masks(maskIndex))
        Next

        allRegionMask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)

        Dim incr = 255 / basics.masks.Count
        palette.src = dst2 * cv.Scalar.All(incr) ' spread the colors 
        palette.Run(ocvb)
        dst1.SetTo(0)
        palette.dst1.CopyTo(dst1, allRegionMask)

        label2 = CStr(basics.masks.Count) + " regions > " + CStr(basics.minFloodSize) + " pixels"
        If standalone Then dst2 = palette.colorMat.Resize(src.Size())
    End Sub
End Class






Public Class FloodFill_Top16_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "FloodFill Minimum Size", 1, 5000, 2000)
        sliders.setupTrackBar(1, "FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar(2, "FloodFill HiDiff", 1, 255, 5)

        ocvb.desc = "Use floodfill to build image segments with a grayscale image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim minFloodSize = sliders.trackbar(0).Value
        Dim loDiff = cv.Scalar.All(sliders.trackbar(1).Value)
        Dim hiDiff = cv.Scalar.All(sliders.trackbar(2).Value)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone()
        grid.Run(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim nextByte = src.Get(Of Byte)(y, x)
                    If nextByte <> 255 And nextByte > 0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(dst1, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
End Class




Public Class FloodFill_Color_MT
    Inherits VBparent
    Dim flood As FloodFill_Top16_MT
    Dim grid As Thread_Grid
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)
        flood = New FloodFill_Top16_MT(ocvb)

        ocvb.desc = "Use floodfill to build image segments in an RGB image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim minFloodSize = flood.sliders.trackbar(0).Value
        Dim loDiff = cv.Scalar.All(flood.sliders.trackbar(1).Value)
        Dim hiDiff = cv.Scalar.All(flood.sliders.trackbar(2).Value)

        dst1 = src.Clone()
        grid.Run(ocvb)
        Dim vec255 = New cv.Vec3b(255, 255, 255)
        Dim vec0 = New cv.Vec3b(0, 0, 0)
        Dim regionCount As Integer = 0
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim vec = src.Get(Of cv.Vec3b)(y, x)
                    If vec <> vec255 And vec <> vec0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        If count > minFloodSize Then
                            Interlocked.Increment(regionCount)
                            count = cv.Cv2.FloodFill(dst1, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        End If
                    End If
                Next
            Next
        End Sub)
        label1 = CStr(regionCount) + " regions were filled with Floodfill"
    End Sub
End Class




Public Class FloodFill_DCT
    Inherits VBparent
    Dim flood As FloodFill_Color_MT
    Dim dct As DCT_FeatureLess
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        flood = New FloodFill_Color_MT(ocvb)

        dct = New DCT_FeatureLess(ocvb)
        ocvb.desc = "Find surfaces that lack any texture with DCT (highest frequency removed) and use floodfill to isolate those surfaces."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        dct.src = src
        dct.Run(ocvb)

        flood.src = dct.dst2.Clone()
        flood.Run(ocvb)
        dst1 = flood.dst1
    End Sub
End Class






Public Class FloodFill_CComp
    Inherits VBparent
    Dim ccomp As CComp_Basics
    Dim range As FloodFill_RelativeRange
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        shadow = New Depth_Holes(ocvb)
        ccomp = New CComp_Basics(ocvb)
        range = New FloodFill_RelativeRange(ocvb)

        label1 = "Input to Floodfill "
        ocvb.desc = "Use Floodfill with the output of the connected components to stabilize the colors used."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        ' shadow.Run(ocvb)

        ccomp.src = src
        ccomp.Run(ocvb)

        range.src = ccomp.dst1
        ' range.fBasics.initialMask = shadow.holeMask
        range.Run(ocvb)
        dst1 = range.dst1
        dst2 = range.dst2
        label2 = CStr(ccomp.connectedComponents.blobs.length) + " blobs found. " + CStr(range.fBasics.rects.Count) + " were more than " +
                      CStr(range.fBasics.sliders.trackbar(0).Value) + " pixels"
    End Sub
End Class





Public Class FloodFill_RelativeRange
    Inherits VBparent
    Public fBasics As FloodFill_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fBasics = New FloodFill_Basics(ocvb)
        check.Setup(ocvb, caller, 3)
        check.Box(0).Text = "Use Fixed range - when off, it means use relative range "
        check.Box(1).Text = "Use 4 nearest pixels (Link4) - when off, it means use 8 nearest pixels (Link8)"
        check.Box(1).Checked = True ' link4 produces better results.
        check.Box(2).Text = "Use 'Mask Only'"
        label1 = "Input to floodfill basics"
        label2 = "Output of floodfill basics"
        ocvb.desc = "Experiment with 'relative' range option to floodfill.  Compare to fixed range option."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fBasics.floodFlag = 0
        If check.Box(0).Checked Then fBasics.floodFlag += cv.FloodFillFlags.FixedRange
        If check.Box(1).Checked Then fBasics.floodFlag += cv.FloodFillFlags.Link4 Else fBasics.floodFlag += cv.FloodFillFlags.Link8
        If check.Box(2).Checked Then fBasics.floodFlag += cv.FloodFillFlags.MaskOnly
        fBasics.src = src
        fBasics.Run(ocvb)
        dst1 = src
        dst2 = fBasics.dst2
    End Sub
End Class




Public Class FloodFill_Top16
    Inherits VBparent
    Public flood As FloodFill_Basics

    Public thumbNails As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Show (up to) the first 16 largest objects in view (in order of size)"

        flood = New FloodFill_Basics(ocvb)

        label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.src = src

        thumbNails = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        Dim allSize = New cv.Size(thumbNails.Width / 4, thumbNails.Height / 4) ' show the first 16 masks

        flood.Run(ocvb)

        dst1.SetTo(0)
        Dim thumbCount As Integer
        Dim allRect = New cv.Rect(0, 0, allSize.Width, allSize.Height)
        For i = 0 To flood.masks.Count - 1
            Dim maskIndex = flood.maskSizes.ElementAt(i).Value
            Dim nextColor = ocvb.scalarColors(i Mod 255)
            dst1.SetTo(nextColor, flood.masks(maskIndex))
            If thumbCount < 16 Then
                thumbNails(allRect) = flood.masks(maskIndex).Resize(allSize).Threshold(0, 255, cv.ThresholdTypes.Binary)
                thumbNails.Rectangle(allRect, cv.Scalar.White, 1)
                allRect.X += allSize.Width
                If allRect.X >= thumbNails.Width Then
                    allRect.X = 0
                    allRect.Y += allSize.Height
                End If
                thumbCount += 1
            End If
        Next
        If check.Box(0).Checked Then dst1 = thumbNails.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        label1 = CStr(flood.masks.Count) + " regions > " + CStr(flood.minFloodSize) + " pixels"
    End Sub
End Class






Public Class Floodfill_Objects
    Inherits VBparent
    Dim basics As FloodFill_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Desired number of objects", 1, 100, 30)

        basics = New FloodFill_Basics(ocvb)
        basics.sliders.trackbar(0).Value = (src.Width Mod 100) * 25

        ocvb.desc = "Use floodfill to identify the desired number of objects"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        basics.src = src
        basics.Run(ocvb)
        dst1 = basics.dst2

        label1 = CStr(basics.masks.Count) + " objects with more than " + CStr(basics.sliders.trackbar(0).Value) + " bytes"
        Static lastSetting As Integer = basics.sliders.trackbar(1).Value
        If dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero() < 0.9 * basics.src.Total And basics.sliders.trackbar(0).Value > 500 Then
            basics.sliders.trackbar(0).Value -= 10
        Else
            If basics.masks.Count >= sliders.trackbar(0).Value Then
                If basics.sliders.trackbar(1).Value < basics.sliders.trackbar(1).Maximum Then basics.sliders.trackbar(1).Value += 1
                If basics.sliders.trackbar(2).Value < basics.sliders.trackbar(2).Maximum Then basics.sliders.trackbar(2).Value += 1
            Else
                If basics.sliders.trackbar(1).Value > 1 Then
                    basics.sliders.trackbar(1).Value -= 1
                    basics.sliders.trackbar(2).Value -= 1
                End If
            End If
            lastSetting = basics.sliders.trackbar(1).Value
        End If
    End Sub
End Class





Public Class FloodFill_WithDepth
    Inherits VBparent
    Dim range As FloodFill_RelativeRange
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        shadow = New Depth_Holes(ocvb)

        range = New FloodFill_RelativeRange(ocvb)

        label1 = "Floodfill results after removing unknown depth"
        label2 = "Mask showing where depth data is missing"
        ocvb.desc = "Floodfill only the areas where there is depth"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        shadow.Run(ocvb)

        range.src = src
        range.fBasics.initialMask = shadow.holeMask
        range.Run(ocvb)
        dst1 = range.dst2
        dst2 = shadow.holeMask
    End Sub
End Class






Public Class Floodfill_Identifiers
    Inherits VBparent
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public minFloodSize As Integer
    Public basics As FloodFill_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        basics = New FloodFill_Basics(ocvb)
        label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill on a projection to determine how many objects and where they are - needs more work"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        minFloodSize = basics.sliders.trackbar(0).Value
        Dim loDiff = cv.Scalar.All(basics.sliders.trackbar(1).Value)
        Dim hiDiff = cv.Scalar.All(basics.sliders.trackbar(2).Value)
        Dim stepSize = basics.sliders.trackbar(3).Value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone()
        Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1)

        rects.Clear()
        centroids.Clear()
        masks.Clear()
        dst2.SetTo(0)
        cv.Cv2.BitwiseNot(src, src)
        For y = 0 To src.Height - 1 Step stepSize
            For x = 0 To src.Width - 1 Step stepSize
                If src.Get(Of Byte)(y, x) < 255 Then
                    Dim rect As New cv.Rect
                    maskPlus.SetTo(0)
                    Dim count = cv.Cv2.FloodFill(src, maskPlus, New cv.Point(x, y), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
                    If count > minFloodSize Then
                        rects.Add(rect)
                        masks.Add(maskPlus(rect).Clone())
                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
                        centroids.Add(centroid)
                    End If
                End If
            Next
        Next

        label2 = CStr(rects.Count) + " regions > " + CStr(minFloodSize) + " pixels"

        For i = 0 To masks.Count - 1
            Dim rect = rects(i)
            dst2(rect).SetTo(ocvb.scalarColors(i Mod 255), masks(i))
        Next
    End Sub
End Class






Public Class Floodfill_ColorObjects
    Inherits VBparent
    Public pFlood As Floodfill_Identifiers
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        pFlood = New Floodfill_Identifiers(ocvb)

        ocvb.desc = "Use floodfill to identify each of the region candidates using only color."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        pFlood.src = src
        pFlood.Run(ocvb)
        dst1 = pFlood.dst2.Clone

        masks = New List(Of cv.Mat)(pFlood.masks)
        rects = New List(Of cv.Rect)(pFlood.rects)
        centroids = New List(Of cv.Point2f)(pFlood.centroids)

        For i = 0 To pFlood.masks.Count - 1
            masks.Add(pFlood.masks(i))
            rects.Add(pFlood.rects(i))
            centroids.Add(pFlood.centroids(i))
        Next
    End Sub
End Class





Public Class FloodFill_PointTracker
    Inherits VBparent
    Dim pTrack As Kalman_PointTracker
    Dim flood As FloodFill_8bit
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        pTrack = New Kalman_PointTracker(ocvb)
        flood = New FloodFill_8bit(ocvb)

        label1 = "Point tracker output"
        ocvb.desc = "Test the FloodFill output as input into the point tracker"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        flood.src = src
        flood.Run(ocvb)
        dst2 = flood.dst1

        pTrack.queryPoints = flood.basics.centroids
        pTrack.queryRects = flood.basics.rects
        pTrack.queryMasks = flood.basics.masks
        pTrack.Run(ocvb)

        label2 = CStr(pTrack.viewObjects.Count) + " regions were found"
        dst1 = pTrack.dst1
    End Sub
End Class
