Imports cv = OpenCvSharp

Module Draw_Exports
    Dim rng As System.Random
    Public Sub drawRotatedRectangle(rotatedRect As cv.RotatedRect, dst1 As cv.Mat, color As cv.Scalar)
        Dim vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        cv.Cv2.FillConvexPoly(dst1, vertices, color, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub drawRotatedOutline(rr As cv.RotatedRect, dst1 As cv.Mat, color As cv.Scalar)
        Dim vertices = rr.Points()
        For i = 0 To 4 - 1
            dst1.Line(New cv.Point(vertices(i).X, vertices(i).Y), New cv.Point(vertices((i + 1) Mod 4).X, vertices((i + 1) Mod 4).Y), cv.Scalar.Black, 1, cv.LineTypes.AntiAlias)
        Next
        dst1.Rectangle(rr.BoundingRect, cv.Scalar.Black, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Function initRandomRect(width As Integer, height As Integer, margin As Integer) As cv.Rect
        Dim x As Integer, y As Integer, w As Integer, h As Integer
        While 1
            x = (width - margin * 2) * Rnd() + margin
            w = (width - x - margin * 2) * Rnd()
            If w > 5 Then Exit While  ' don't let the width/height get too small...
        End While

        While 1
            y = (height - margin * 2) * Rnd() + margin
            h = (height - y - margin * 2) * Rnd()
            If h > 5 Then Exit While  ' don't let the width/height get too small...
        End While

        Return New cv.Rect(x, y, w, h)
    End Function
End Module




Public Class Draw_rectangles
    Inherits VBparent
    Public updateFrequency = 30
    Public drawRotatedRectangles As Boolean
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Rectangle Count", 1, 255, 3)
        ocvb.desc = "Draw the requested number of rotated rectangles."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.trackbar(0).Value - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim width = msRNG.Next(0, src.Cols - nPoint.X - 1)
                Dim height = msRNG.Next(0, src.Rows - nPoint.Y - 1)
                Dim eSize = New cv.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)
                Dim rotatedRect = New cv.RotatedRect(nPoint, eSize, angle)

                Dim nextColor = New cv.Scalar(ocvb.vecColors(i).Item0, ocvb.vecColors(i).Item1, ocvb.vecColors(i).Item2)
                If drawRotatedRectangles Then
                    drawRotatedRectangle(rotatedRect, dst1, nextColor)
                Else
                    cv.Cv2.Rectangle(dst1, New cv.Rect(nPoint.X, nPoint.Y, width, height), nextColor, -1)
                End If
            Next
        End If
    End Sub
End Class



Public Class Draw_Noise
    Inherits VBparent
    Public maxNoiseWidth As integer = 3
    Public addRandomColor As Boolean
    Public noiseMask As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Noise Count", 1, 1000, 100)
        sliders.setupTrackBar(1, "Noise Width", 1, 10, 3)
        ocvb.desc = "Add Noise to the color image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        maxNoiseWidth = sliders.trackbar(1).Value
        src.CopyTo(dst1)
        noiseMask = New cv.Mat(src.Size(), cv.MatType.CV_8UC1).SetTo(0)
        For n = 0 To sliders.trackbar(0).Value
            Dim i = msRNG.Next(0, src.Cols - 1)
            Dim j = msRNG.Next(0, src.Rows - 1)
            Dim center = New cv.Point2f(i, j)
            Dim c = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            If addRandomColor = False Then c = cv.Scalar.Black
            Dim noiseWidth = msRNG.Next(1, maxNoiseWidth)
            dst1.Circle(center, noiseWidth, c, -1, cv.LineTypes.AntiAlias)
            noiseMask.Circle(center, noiseWidth, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class



Public Class Draw_rotatedRectangles
    Inherits VBparent
    Public rect As Draw_rectangles
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        rect = New Draw_rectangles(ocvb)
        rect.drawRotatedRectangles = True
        ocvb.desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        rect.src = src
        rect.Run(ocvb)
        dst1 = rect.dst1
    End Sub
End Class



Public Class Draw_Ellipses
    Inherits VBparent
    Public updateFrequency = 30
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Ellipse Count", 1, 255, 3)
        ocvb.desc = "Draw the requested number of ellipses."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.trackbar(0).Value - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim eSize = New cv.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)
                Dim nextColor = New cv.Scalar(ocvb.vecColors(i).Item0, ocvb.vecColors(i).Item1, ocvb.vecColors(i).Item2)
                dst1.Ellipse(New cv.RotatedRect(nPoint, eSize, angle), nextColor, -1,)
            Next
        End If
    End Sub
End Class



Public Class Draw_Circles
    Inherits VBparent
    Public updateFrequency = 30
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Circle Count", 1, 255, 3)
        ocvb.desc = "Draw the requested number of circles."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.trackbar(0).Value - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim radius = msRNG.Next(10, 10 + msRNG.Next(src.Cols / 4))
                Dim nextColor = New cv.Scalar(ocvb.vecColors(i).Item0, ocvb.vecColors(i).Item1, ocvb.vecColors(i).Item2)
                dst1.Circle(nPoint, radius, nextColor, -1,)
            Next
        End If
    End Sub
End Class



Public Class Draw_Line
    Inherits VBparent
    Public updateFrequency = 30
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Line Count", 1, 255, 1)
        ocvb.desc = "Draw the requested number of Lines."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount Mod updateFrequency Then Exit Sub
        dst1.SetTo(cv.Scalar.White)
        For i = 0 To sliders.trackbar(0).Value - 1
            Dim nPoint1 = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
            Dim nPoint2 = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
            Dim thickness = msRNG.Next(1, 10)
            Dim nextColor = New cv.Scalar(ocvb.vecColors(i).Item0, ocvb.vecColors(i).Item1, ocvb.vecColors(i).Item2)
            dst1.Line(nPoint1, nPoint2, nextColor, thickness, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class



Public Class Draw_Polygon
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Poly Count", 1, 255, 1)
        ocvb.desc = "Draw Polygon figures"
        label2 = "Convex Hull for the same polygon"

        radio.Setup(ocvb, caller, 2) ' ask for 2 radio buttons
        radio.check(0).Text = "Polygon Outline"
        radio.check(1).Text = "Polygon Filled"
        radio.check(0).Checked = True
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim height = src.Height / 8
        Dim width = src.Width / 8
        Dim polyColor = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
        dst1.SetTo(cv.Scalar.White)
        dst2 = dst1.Clone()
        For i = 0 To sliders.trackbar(0).Value - 1
            Dim points = New List(Of cv.Point)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            For j = 0 To 10
                points.Add(New cv.Point(CInt(msRNG.Next(width, width * 7)), CInt(msRNG.Next(height, height * 7))))
            Next
            listOfPoints.Add(points)
            If radio.check(0).Checked Then
                cv.Cv2.Polylines(dst1, listOfPoints, True, polyColor, 2, cv.LineTypes.AntiAlias)
            Else
                dst1.FillPoly(listOfPoints, New cv.Scalar(0, 0, 255))
            End If

            Dim hull() As cv.Point
            hull = cv.Cv2.ConvexHull(points, True)
            listOfPoints = New List(Of List(Of cv.Point))
            points = New List(Of cv.Point)
            For j = 0 To hull.Count - 1
                points.Add(New cv.Point(hull(j).X, hull(j).Y))
            Next
            listOfPoints.Add(points)
            dst2.SetTo(cv.Scalar.White)
            If radio.check(0).Checked Then
                cv.Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, 2)
            Else
                cv.Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, -1)
            End If
        Next
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class Draw_RngImage
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Use RNG to draw the same set of shapes every time"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim offsetX = 50, offsetY = 25, lineLength = 50, thickness = 2

        dst1.SetTo(cv.Scalar.White)
        For i = 1 To 256
            dst1.Line(New cv.Point(thickness * i + offsetX, offsetY), New cv.Point(thickness * i + offsetX, offsetY + lineLength), New cv.Scalar(i, i, i), thickness)
        Next
        For i = 1 To 256
            Dim color = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
            Select Case msRNG.Next(0, 3)
                Case 0 ' circle
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst1.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim radius = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    dst1.Circle(center, radius, color, -1, cv.LineTypes.Link8)
                Case 1 ' Rectangle
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst1.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim rc = New cv.Rect(center.X - width, center.Y - height / 2, width, height)
                    dst1.Rectangle(rc, color, -1, cv.LineTypes.Link8)
                Case 2 ' Ellipse
                    Dim center = New cv.Point(msRNG.Next(offsetX, dst1.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim width = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim height = msRNG.Next(1, Math.Min(offsetX, offsetY))
                    Dim angle = msRNG.Next(0, 180)
                    dst1.Ellipse(center, New cv.Size(width / 2, height / 2), angle, 0, 360, color, -1, cv.LineTypes.Link8)
            End Select
        Next
    End Sub
End Class





Public Class Draw_SymmetricalShapes
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of points", 200, 1000, 500)
        sliders.setupTrackBar(1, "Radius 1", 1, src.Rows / 2, src.Rows / 4)
        sliders.setupTrackBar(2, "Radius 2", 1, src.Rows / 2, src.Rows / 8)
        sliders.setupTrackBar(3, "nGenPer", 1, 500, 100)

        check.Setup(ocvb, caller, 5)
        check.Box(0).Text = "Symmetric Ripple"
        check.Box(1).Text = "Only Regular Shapes"
        check.Box(2).Text = "Filled Shapes"
        check.Box(3).Text = "Reverse In/Out"
        check.Box(4).Text = "Use demo mode"
        check.Box(4).Checked = True
        ocvb.desc = "Generate shapes programmatically"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static rotateAngle As Single = 0
        Static fillColor = cv.Scalar.Red
        If check.Box(4).Checked Then
            If ocvb.frameCount Mod 30 = 0 Then
                If sliders.trackbar(0).Value < sliders.trackbar(0).Maximum - 17 Then sliders.trackbar(0).Value += 17 Else sliders.trackbar(0).Value = sliders.trackbar(0).Minimum
                If sliders.trackbar(1).Value < sliders.trackbar(1).Maximum - 10 Then sliders.trackbar(1).Value += 10 Else sliders.trackbar(1).Value = 1
                If sliders.trackbar(2).Value > 13 Then sliders.trackbar(2).Value -= 13 Else sliders.trackbar(2).Value = sliders.trackbar(2).Maximum
                If sliders.trackbar(3).Value > 27 Then sliders.trackbar(3).Value -= 27 Else sliders.trackbar(3).Value = sliders.trackbar(3).Maximum
                fillColor = ocvb.scalarColors(ocvb.frameCount Mod 255)
            End If
            If ocvb.frameCount Mod 37 = 0 Then check.Box(0).Checked = Not check.Box(0).Checked
            If ocvb.frameCount Mod 222 = 0 Then check.Box(1).Checked = Not check.Box(1).Checked
            If ocvb.frameCount Mod 77 = 0 Then check.Box(2).Checked = Not check.Box(2).Checked
            If ocvb.frameCount Mod 100 = 0 Then check.Box(3).Checked = Not check.Box(3).Checked
            rotateAngle += 1

        End If

        dst1.SetTo(cv.Scalar.White)
        Dim numPoints = sliders.trackbar(0).Value
        Dim nGenPer = sliders.trackbar(3).Value
        If check.Box(1).Checked Then numPoints = CInt(numPoints / nGenPer) * nGenPer ' harmonize
        Dim radius1 = sliders.trackbar(1).Value
        Dim radius2 = sliders.trackbar(2).Value
        Dim dTheta = 2 * cv.Cv2.PI / numPoints
        Dim symmetricRipple = check.Box(0).Checked
        Dim reverseInOut = check.Box(3).Checked
        Dim pt As New cv.Point
        Dim center As New cv.Point(src.Width / 2, src.Height / 2)
        Dim points As New List(Of cv.Point)

        For i = 0 To numPoints - 1
            Dim theta = i * dTheta
            Dim ripple = radius2 * Math.Cos(nGenPer * theta)
            If symmetricRipple = False Then ripple = Math.Abs(ripple)
            If reverseInOut Then ripple = -ripple
            pt.X = Math.Truncate(center.X + (radius1 + ripple) * Math.Cos(theta + rotateAngle) + 0.5)
            pt.Y = Math.Truncate(center.Y - (radius1 + ripple) * Math.Sin(theta + rotateAngle) + 0.5)
            points.Add(pt)
        Next

        For i = 0 To numPoints - 1
            dst1.Line(points.ElementAt(i), points.ElementAt((i + 1) Mod numPoints), ocvb.scalarColors(i Mod ocvb.scalarColors.Count), 2, cv.LineTypes.AntiAlias)
        Next

        If check.Box(2).Checked Then dst1.FloodFill(center, fillColor)
    End Sub
End Class





Public Class Draw_ClipLine
    Inherits VBparent
    Dim flow As Font_FlowText
    Dim kalman As Kalman_Basics
    Dim lastRect As cv.Rect
    Dim pt1 As cv.Point
    Dim pt2 As cv.Point
    Dim rect As cv.Rect
    Private Sub setup()
        ReDim kalman.input(8)
        Dim r = initRandomRect(dst2.Width, dst2.Height, 25)
        pt1 = New cv.Point(r.X, r.Y)
        pt2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
        rect = initRandomRect(dst2.Width, dst2.Height, 25)
        If kalman.check.Box(0).Checked Then flow.msgs.Add("--------------------------- setup ---------------------------")
    End Sub
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        flow = New Font_FlowText(ocvb)

        kalman = New Kalman_Basics(ocvb)
        setup()

        ocvb.desc = "Demonstrate the use of the ClipLine function in OpenCV. NOTE: when clipline returns true, p1/p2 are clipped by the rectangle"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        dst2 = src
        kalman.input = {pt1.X, pt1.Y, pt2.X, pt2.Y, rect.X, rect.Y, rect.Width, rect.Height}
        kalman.Run(ocvb)
        Dim p1 = New cv.Point(CInt(kalman.output(0)), CInt(kalman.output(1)))
        Dim p2 = New cv.Point(CInt(kalman.output(2)), CInt(kalman.output(3)))

        If kalman.output(6) < 5 Then kalman.output(6) = 5 ' don't let the width/height get too small...
        If kalman.output(7) < 5 Then kalman.output(7) = 5
        Dim r = New cv.Rect(kalman.output(4), kalman.output(5), kalman.output(6), kalman.output(7))

        Dim clipped = cv.Cv2.ClipLine(r, p1, p2) ' Returns false when the line and the rectangle don't intersect.
        dst2.Line(p1, p2, If(clipped, cv.Scalar.White, cv.Scalar.Black), 2, cv.LineTypes.AntiAlias)
        dst2.Rectangle(r, If(clipped, cv.Scalar.Yellow, cv.Scalar.Red), 2, cv.LineTypes.AntiAlias)

        Static linenum = 0
        flow.msgs.Add("(" + CStr(linenum) + ") line " + If(clipped, "interects rectangle", "does not intersect rectangle"))
        linenum += 1

        Static hitCount = 0
        hitCount += If(clipped, 1, 0)
        ocvb.trueText("There were " + Format(hitCount, "###,##0") + " intersects and " + Format(linenum - hitCount) + " misses",
                     CInt(src.Width / 2), 200)
        If r = rect Then setup()
        flow.Run(ocvb)
    End Sub
End Class






Public Class Draw_Arc
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Dim saveArcAngle As Integer
    Dim saveMargin As Integer
    Dim rect As cv.Rect

    Dim angle As Single
    Dim startAngle As Single
    Dim endAngle As Single

    Dim colorIndex As Integer
    Dim thickness As Integer
    Private Sub setup(ocvb As VBocvb)
        saveMargin = sliders.trackbar(0).Value ' work in the middle of the image.

        rect = initRandomRect(dst1.Width, dst1.Height, saveMargin)
        angle = msRNG.Next(0, 360)
        colorIndex = msRNG.Next(0, 255)
        thickness = msRNG.Next(1, 5)
        startAngle = msRNG.Next(1, 360)
        endAngle = msRNG.Next(1, 360)

        kalman.input = {rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle}
    End Sub
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(7 - 1)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Clearance from image edge (margin size)", 5, src.Width / 8, src.Width / 16)
        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Draw Full Ellipse"
        radio.check(1).Text = "Draw Filled Arc"
        radio.check(2).Text = "Draw Arc"
        radio.check(1).Checked = True

        setup(ocvb)

        ocvb.desc = "Use OpenCV's ellipse function to draw an arc"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If kalman.check.Box(0).Checked Then
            kalman.input = {rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle}
            kalman.Run(ocvb)
        Else
            kalman.output = kalman.input ' do nothing...
        End If
        Dim r = New cv.Rect(kalman.output(0), kalman.output(1), kalman.output(2), kalman.output(3))
        If r.Width <= 5 Then r.Width = 5
        If r.Height <= 5 Then r.Height = 5
        Dim rr = New cv.RotatedRect(New cv.Point2f(r.X, r.Y), New cv.Size2f(r.Width, r.Height), angle)
        Dim color = ocvb.scalarColors(colorIndex)

        dst1.SetTo(cv.Scalar.White)
        If radio.check(0).Checked Then
            dst1.Ellipse(rr, color, thickness, cv.LineTypes.AntiAlias)
            drawRotatedOutline(rr, dst1, ocvb.scalarColors(colorIndex))
        Else
            Dim angle = kalman.output(4)
            Dim startAngle = kalman.output(5)
            Dim endAngle = kalman.output(6)
            If radio.check(1).Checked Then thickness = -1
            dst1.Ellipse(New cv.Point(rr.Center.X, rr.Center.Y), New cv.Size(rr.BoundingRect.Size.Width, rr.BoundingRect.Size.Height),
                         angle, startAngle, endAngle, color, thickness, cv.LineTypes.AntiAlias)
        End If
        If r = rect Or sliders.trackbar(0).Value <> saveMargin Then setup(ocvb)
    End Sub
End Class





Public Class Draw_OverlappingRectangles
    Inherits VBparent
    Dim flood As FloodFill_Basics
    Public inputRects As New List(Of cv.Rect)
    Public inputMasks As New List(Of cv.Mat)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        If standalone Then flood = New FloodFill_Basics(ocvb)

        label1 = "(First 5) Overlapping rectangles are red, original in yellow"
        label2 = "Original list of rectangles"
        ocvb.desc = "Find first 5 rectangles that are overlapping."
    End Sub
    Private Class CompareMasks : Implements IComparer(Of cv.Rect)
        Public Function Compare(ByVal a As cv.Rect, ByVal b As cv.Rect) As Integer Implements IComparer(Of cv.Rect).Compare
            If a.Width * a.Height > b.Width * b.Height Then Return 1
            Return -1 ' never returns equal because duplicates can happen.
        End Function
    End Class
    Private Function overlapping(r1 As cv.Rect, r2 As cv.Rect)
        Dim w = r1.Width
        Dim h = r1.Height
        For i = 0 To 4 - 1
            Dim p1 = Choose(i + 1, New cv.Point(r1.X, r1.Y), New cv.Point(r1.X + w, r1.Y), New cv.Point(r1.X, r1.Y + h), New cv.Point(r1.X + w, r1.Y + h))
            If p1.x >= r2.X And p1.x <= r2.X + r2.Width And p1.y >= r2.Y And p1.y <= r2.Y + r2.Height Then Return True
            Dim p2 = Choose(i + 1, New cv.Point(r1.X + w, r1.Y), New cv.Point(r1.X, r1.Y + h), New cv.Point(r1.X + w, r1.Y + h), New cv.Point(r1.X + w, r1.Y + h))
        Next
        Return False
    End Function
    Private Function addOverlapping(overlapping As List(Of cv.Rect), r As cv.Rect) As List(Of cv.Rect)
        For Each rect In overlapping
            If rect.X = r.X And rect.Y = r.Y Then
                overlapping.Remove(rect)
                r.Width = Math.Max(rect.Width, r.Width)
                r.Height = Math.Max(rect.Height, r.Height)
                Exit For
            End If
        Next
        overlapping.Add(r)
        Return overlapping
    End Function
    Public Sub Run(ocvb As VBocvb)
        If standalone Then
            flood.src = src
            flood.Run(ocvb)
            dst1 = flood.dst2
            inputRects.Clear()
            inputMasks.Clear()

            For i = 0 To Math.Min(5, flood.rects.Count) - 1
                inputRects.Add(flood.rects(i))
                inputMasks.Add(flood.masks(i))
            Next
        End If

        dst2.SetTo(0)
        For Each r In inputRects
            dst1.Rectangle(r, cv.Scalar.Yellow, 4)
            dst2.Rectangle(r, cv.Scalar.Yellow, 1)
        Next

        Dim removeRects As New List(Of cv.Rect)
        Dim overlappingRects As New List(Of cv.Rect)

        For i = 0 To inputRects.Count - 1
            Dim r1 = inputRects(i)
            For j = i + 1 To inputRects.Count - 1
                Dim r2 = inputRects(j)
                Dim overlap1 = overlapping(r1, r2)
                Dim overlap2 = overlapping(r2, r1)
                If overlap1 Or overlap2 Then
                    Dim pt1 = New cv.Point(Math.Min(r1.X, r2.X), Math.Min(r1.Y, r2.Y))
                    Dim pt2 = New cv.Point(Math.Max(r1.X + r1.Width, r2.X + r2.Width), Math.Max(r1.Y + r1.Height, r2.Y + r2.Height))
                    Dim rect = New cv.Rect(pt1.X, pt1.Y, pt2.X - pt1.X, pt2.Y - pt1.Y)
                    overlappingRects = addOverlapping(overlappingRects, rect)
                    If removeRects.Contains(r1) = False Then removeRects.Add(r1)
                    If removeRects.Contains(r2) = False Then removeRects.Add(r2)
                End If
            Next
        Next
        For Each r In removeRects
            inputRects.Remove(r)
        Next
        For Each r In overlappingRects
            inputRects.Add(r)
        Next

        For Each r In inputRects
            dst1.Rectangle(r, cv.Scalar.Red, 1)
        Next
    End Sub
End Class
