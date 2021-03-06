Imports cv = OpenCvSharp

Public Class Contours_Basics
    Inherits VBparent
    Public rotatedRect As Draw_rotatedRectangles
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        radio.Setup(ocvb, caller, 5)
        radio.Text = "Retrieval Mode Options"
        radio.check(0).Text = "CComp"
        radio.check(1).Text = "External"
        radio.check(2).Text = "FloodFill"
        radio.check(3).Text = "List"
        radio.check(4).Text = "Tree"
        radio.check(4).Checked = True

        radio1.Setup(ocvb, caller, 4)
        radio1.Text = "ContourApproximation Mode"
        radio1.check(0).Text = "ApproxNone"
        radio1.check(1).Text = "ApproxSimple"
        radio1.check(2).Text = "ApproxTC89KCOS"
        radio1.check(3).Text = "ApproxTC89L1"
        radio1.check(1).Checked = True

        rotatedRect = New Draw_rotatedRectangles(ocvb)
        rotatedRect.rect.sliders.trackbar(0).Value = 5
        ocvb.desc = "Demo options on FindContours."
        label2 = "FindContours output"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim retrievalMode As cv.RetrievalModes
        Dim ApproximationMode As cv.ContourApproximationModes
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External, cv.RetrievalModes.FloodFill, cv.RetrievalModes.List, cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        For i = 0 To radio1.check.Count - 1
            If radio1.check(i).Checked Then
                ApproximationMode = Choose(i + 1, cv.ContourApproximationModes.ApproxNone, cv.ContourApproximationModes.ApproxSimple,
                                              cv.ContourApproximationModes.ApproxTC89KCOS, cv.ContourApproximationModes.ApproxTC89L1)
                Exit For
            End If
        Next

        Dim imageInput As New cv.Mat
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If standalone Then
            rotatedRect.src = src
            rotatedRect.Run(ocvb)
            imageInput = rotatedRect.dst1
            If imageInput.Channels = 3 Then
                src = imageInput.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(254, 255, cv.ThresholdTypes.BinaryInv)
            Else
                src = imageInput.Threshold(254, 255, cv.ThresholdTypes.BinaryInv)
            End If
        End If

        Dim contours0 As cv.Point()()
        If retrievalMode = cv.RetrievalModes.FloodFill Then
            '    Dim img32sc1 As New cv.Mat
            '    src.ConvertTo(img32sc1, cv.MatType.CV_32SC1)
            '    contours0 = cv.Cv2.FindContoursAsArray(img32sc1, retrievalMode, ApproximationMode)
            '    img32sc1.ConvertTo(dst1, cv.MatType.CV_8UC1)
            contours0 = cv.Cv2.FindContoursAsArray(src, cv.RetrievalModes.Tree, ApproximationMode)
        Else
            contours0 = cv.Cv2.FindContoursAsArray(src, retrievalMode, ApproximationMode)
        End If

        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
        Next

        dst1 = imageInput
        dst2.SetTo(0)
        If retrievalMode = cv.RetrievalModes.FloodFill Then
            cv.Cv2.DrawContours(dst2, contours, 0, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Else
            cv.Cv2.DrawContours(dst2, contours, 0, cv.Scalar.Yellow, 2, cv.LineTypes.AntiAlias)
        End If
    End Sub
End Class



Public Class Contours_FindandDraw
    Inherits VBparent
    Dim rotatedRect As Draw_rotatedRectangles
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        rotatedRect = New Draw_rotatedRectangles(ocvb)
        rotatedRect.rect.sliders.trackbar(0).Value = 5
        label1 = "FindandDraw input"
        label2 = "FindandDraw output"
        ocvb.desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim img As New cv.Mat(dst1.Size(), cv.MatType.CV_8UC1)
        rotatedRect.src = src
        rotatedRect.Run(ocvb)
        dst1 = rotatedRect.dst1
        img = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(254, 255, cv.ThresholdTypes.BinaryInv)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
        Next

        dst2.SetTo(0)
        cv.Cv2.DrawContours(dst2, contours, 0, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
    End Sub
End Class



Public Class Contours_Depth
    Inherits VBparent
    Public trim As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        trim = New Depth_InRange(ocvb)
        ocvb.desc = "Find and draw the contour of the depth foreground."
        label1 = "DepthContour input"
        label2 = "DepthContour output"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        dst1 = trim.dst1
        dst2.SetTo(0)
        Dim contours0 = cv.Cv2.FindContoursAsArray(trim.Mask, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As integer
        Dim maxNodes As integer
        For i = 0 To contours0.Length - 1
            Dim contours = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < contours.Length Then
                maxIndex = i
                maxNodes = contours.Length
            End If
        Next
        cv.Cv2.DrawContours(dst2, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
    End Sub
End Class



Public Class Contours_RGB
    Inherits VBparent
    Dim trim As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        trim = New Depth_InRange(ocvb)
        ocvb.desc = "Find and draw the contour of the largest foreground RGB contour."
        label2 = "Background"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        Dim img = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        img.SetTo(0, trim.zeroMask)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As integer
        Dim maxNodes As integer
        For i = 0 To contours0.Length - 1
            Dim contours = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < contours.Length Then
                maxIndex = i
                maxNodes = contours.Length
            End If
        Next

        If contours0(maxIndex).Length = 0 Then Exit Sub

        Dim hull() = cv.Cv2.ConvexHull(contours0(maxIndex), True)
        Dim listOfPoints = New List(Of List(Of cv.Point))
        Dim points = New List(Of cv.Point)
        For i = 0 To hull.Count - 1
            points.Add(New cv.Point(hull(i).X, hull(i).Y))
        Next
        listOfPoints.Add(points)
        cv.Cv2.DrawContours(dst1, listOfPoints, 0, New cv.Scalar(255, 0, 0), -1)
        cv.Cv2.DrawContours(dst1, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
        dst2.SetTo(0)
        src.CopyTo(dst2, trim.zeroMask)
    End Sub
End Class





' https://github.com/SciSharp/SharpCV/blob/master/src/SharpCV.Examples/Program.cs
Public Class Contours_RemoveLines
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller, 3)
        sliders.setupTrackBar(0, "Morphology width/height", 1, 100, 20)
        sliders.setupTrackBar(1, "MorphologyEx iterations", 1, 5, 1)
        sliders.setupTrackBar(2, "Contour thickness", 1, 10, 3)
        label1 = "Original image"
        label2 = "Original with horizontal/vertical lines removed"
        ocvb.desc = "Remove the lines from an invoice image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim tmp = cv.Cv2.ImRead(ocvb.parms.homeDir + "Data/invoice.jpg")
        Dim dstSize = New cv.Size(src.Height / tmp.Height * src.Width, src.Height)
        Dim dstRect = New cv.Rect(0, 0, dstSize.Width, src.Height)
        dst1(dstRect) = tmp.Resize(dstSize)
        Dim gray = tmp.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim thresh = gray.Threshold(0, 255, cv.ThresholdTypes.BinaryInv Or cv.ThresholdTypes.Otsu)

        ' remove horizontal lines
        Dim hkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(sliders.trackbar(0).Value, 1))
        Dim removedH As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedH, cv.MorphTypes.Open, hkernel,, sliders.trackbar(1).Value)
        Dim cnts = cv.Cv2.FindContoursAsArray(removedH, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, sliders.trackbar(2).Value)
        Next

        Dim vkernel = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(1, sliders.trackbar(0).Value))
        Dim removedV As New cv.Mat
        cv.Cv2.MorphologyEx(thresh, removedV, cv.MorphTypes.Open, vkernel,, sliders.trackbar(1).Value)
        cnts = cv.Cv2.FindContoursAsArray(removedV, cv.RetrievalModes.External, cv.ContourApproximationModes.ApproxSimple)
        For i = 0 To cnts.Count - 1
            cv.Cv2.DrawContours(tmp, cnts, i, cv.Scalar.White, sliders.trackbar(2).Value)
        Next

        dst2(dstRect) = tmp.Resize(dstSize)
        cv.Cv2.ImShow("Altered image at original resolution", tmp)
    End Sub
End Class