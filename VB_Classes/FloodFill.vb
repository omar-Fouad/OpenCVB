﻿Imports cv = OpenCvSharp



Public Class FloodFill_Basics : Implements IDisposable
    Dim check As New OptionsCheckbox
    Public sliders As New OptionsSliders
    Public srcGray As New cv.Mat
    Public externalUse As Boolean
    Public masks As New List(Of cv.Mat)
    Public maskSizes As New List(Of Int32)
    Public maskRects As New List(Of cv.Rect)
    Public regionNum As Int32
    Public initialMask As New cv.Mat
    Public allMasks As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public Sub New(ocvb As AlgorithmData)
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Show the first 16 masks"
        check.Show()

        sliders.setupTrackBar1(ocvb, "FloodFill Minimum Size", 1, 5000, 2500)
        sliders.setupTrackBar2(ocvb, "FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3(ocvb, "FloodFill HiDiff", 1, 255, 5)
        sliders.setupTrackBar4(ocvb, "Step Size", 1, ocvb.color.Width / 2, 20)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)
        Dim stepSize = sliders.TrackBar4.Value

        If externalUse = False Then
            srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            initialMask = New cv.Mat(ocvb.color.Size, cv.MatType.CV_8U, 0)
        End If
        ocvb.result1 = srcGray.Clone()
        ocvb.result2.SetTo(0)
        Dim rect As New cv.Rect
        Dim maskPlus = New cv.Mat(New cv.Size(srcGray.Width + 2, srcGray.Height + 2), cv.MatType.CV_8UC1)
        Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)
        masks.Clear()
        maskSizes.Clear()
        maskRects.Clear()
        regionNum = 0
        maskPlus.SetTo(0)
        maskPlus(maskRect).SetTo(255, initialMask)
        Dim lastMask = initialMask.Clone() 
        allMasks = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        Dim allSize = New cv.Size(allMasks.Width / 4, allMasks.Height / 4) ' show the first 16 masks
        Dim allRect = New cv.Rect(0, 0, allSize.Width, allSize.Height)
        Dim allCount As Int32
        For y = 0 To srcGray.Height - 1 Step stepSize
            For x = 0 To srcGray.Width - 1 Step stepSize
                Dim seedPoint = New cv.Point(x, y)
                Dim count = cv.Cv2.FloodFill(srcGray, maskPlus, seedPoint, cv.Scalar.White, rect, loDiff, hiDiff, floodFlag)
                If count > minFloodSize Then
                    Dim nextColor = ocvb.colorScalar(regionNum Mod 255)
                    If nextColor = cv.Scalar.All(1) Or nextColor = cv.Scalar.All(0) Then nextColor = ocvb.colorScalar(regionNum Mod 255)
                    regionNum += 1
                    If masks.Count = 0 Then
                        masks.Add(maskPlus(maskRect).Clone())
                    Else
                        masks.Add(maskPlus(maskRect).Clone() - lastMask) ' difference from all previous masks is what we want here.
                    End If
                    masks(masks.Count - 1).SetTo(0, initialMask) ' The initial mask is what should not be part of any mask.
                    ocvb.result2.SetTo(nextColor, masks(masks.Count - 1))
                    If allCount < 16 Then
                        allMasks(allRect) = masks(masks.Count - 1).Resize(allSize).Threshold(0, 255, cv.ThresholdTypes.Binary)
                        allMasks.Rectangle(allRect, cv.Scalar.White, 1)
                        allRect.X += allSize.Width
                        If allRect.X >= allMasks.Width Then
                            allRect.X = 0
                            allRect.Y += allSize.Height
                        End If
                        allCount += 1
                    End If
                    maskSizes.Add(masks(masks.Count - 1).CountNonZero())
                    maskRects.Add(rect)
                    lastMask = maskPlus(maskRect).Clone()
                Else
                    ' or in the unwanted object into the last mask.
                    cv.Cv2.BitwiseOr(lastMask, maskPlus(maskRect), lastMask)
                End If
            Next
        Next
        If check.Box(0).Checked Then ocvb.result2 = allMasks
        ocvb.label2 = CStr(regionNum) + " regions > " + CStr(minFloodSize) + " pixels"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        check.Dispose()
    End Sub
End Class





Public Class FloodFill_Basics_MT : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Public srcGray As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        sliders.setupTrackBar1(ocvb, "FloodFill Minimum Size", 1, 500, 50)
        sliders.setupTrackBar2(ocvb, "FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3(ocvb, "FloodFill HiDiff", 1, 255, 5)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Use floodfill to build image segments with a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)

        If externalUse = False Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = srcGray.Clone()
        grid.Run(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim nextByte = srcGray.At(Of Byte)(y, x)
                    If nextByte <> 255 And nextByte > 0 Then
                        Dim count = cv.Cv2.FloodFill(srcGray, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(ocvb.result2, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class




Public Class FloodFill_Color_MT : Implements IDisposable
    Dim flood As FloodFill_Basics_MT
    Dim grid As Thread_Grid
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        flood = New FloodFill_Basics_MT(ocvb)

        ocvb.desc = "Use floodfill to build image segments in an RGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = flood.sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(flood.sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(flood.sliders.TrackBar3.Value)

        If externalUse = False Then src = ocvb.color.Clone()
        ocvb.result2 = src.Clone()
        grid.Run(ocvb)
        Dim vec255 = New cv.Vec3b(255, 255, 255)
        Dim vec0 = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim vec = src.At(Of cv.Vec3b)(y, x)
                    If vec <> vec255 And vec <> vec0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(ocvb.result2, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        flood.Dispose()
    End Sub
End Class




Public Class FloodFill_DCT : Implements IDisposable
    Dim flood As FloodFill_Color_MT
    Dim dct As DCT_FeatureLess_MT
    Public Sub New(ocvb As AlgorithmData)
        flood = New FloodFill_Color_MT(ocvb)
        flood.externalUse = True

        dct = New DCT_FeatureLess_MT(ocvb)
        ocvb.desc = "Find surfaces that lack any texture with DCT (highest frequency removed) and use floodfill to isolate those surfaces."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dct.Run(ocvb)
        flood.src = ocvb.result2.Clone()
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(ocvb.result1, mask)
        flood.src.SetTo(0, mask)
        flood.Run(ocvb)
        ocvb.result2.SetTo(0, mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        dct.Dispose()
        flood.Dispose()
    End Sub
End Class





Public Class FloodFill_WithDepth : Implements IDisposable
    Dim flood As FloodFill_RelativeRange
    Dim shadow As Depth_Shadow
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Shadow(ocvb)
        shadow.externalUse = True

        flood = New FloodFill_RelativeRange(ocvb)
        flood.fBasics.externalUse = True

        ocvb.desc = "Floodfill only the areas where there is depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        flood.fBasics.srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.fBasics.initialMask = shadow.holeMask
        flood.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        flood.Dispose()
        shadow.Dispose()
    End Sub
End Class





Public Class FloodFill_CComp : Implements IDisposable
    Dim ccomp As CComp_Basics
    Dim flood As FloodFill_RelativeRange
    Dim shadow As Depth_Shadow
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Shadow(ocvb)
        shadow.externalUse = True

        ccomp = New CComp_Basics(ocvb)
        ccomp.externalUse = True

        flood = New FloodFill_RelativeRange(ocvb)
        flood.fBasics.externalUse = True

        ocvb.desc = "Use Floodfill with the output of the connected components to stabilize the colors used."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        ccomp.srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(ocvb)

        flood.fBasics.srcGray = ccomp.dstGray
        flood.fBasics.initialMask = shadow.holeMask
        flood.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ccomp.Dispose()
        flood.Dispose()
        shadow.Dispose()
    End Sub
End Class





Public Class FloodFill_RelativeRange : Implements IDisposable
    Public fBasics As FloodFill_Basics
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        fBasics = New FloodFill_Basics(ocvb)
        check.Setup(ocvb, 3)
        check.Box(0).Text = "Use Fixed range - off means use relative range "
        check.Box(1).Text = "Use 4 nearest pixels (Link4) - off means use 8 nearest pixels (Link8)"
        check.Box(1).Checked = True ' link4 produces better results.
        check.Box(2).Text = "Use 'Mask Only'"
        check.Show()
        ocvb.desc = "Experiment with 'relative' range option to floodfill.  Compare to fixed range option."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        fBasics.floodFlag = 0
        If check.Box(0).Checked Then fBasics.floodFlag += cv.FloodFillFlags.FixedRange
        If check.Box(1).Checked Then fBasics.floodFlag += cv.FloodFillFlags.Link4 Else fBasics.floodFlag += cv.FloodFillFlags.Link8
        If check.Box(2).Checked Then fBasics.floodFlag += cv.FloodFillFlags.MaskOnly
        fBasics.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        check.Dispose()
        fBasics.Dispose()
    End Sub
End Class