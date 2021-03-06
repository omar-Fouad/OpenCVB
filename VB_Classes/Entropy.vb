Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics
    Inherits VBparent
    Dim flow As Font_FlowText
    Dim hist As Histogram_Basics
    Dim simple = New Entropy_Simple
    Public entropy As Single
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        flow = New Font_FlowText(ocvb)

        hist = New Histogram_Basics(ocvb)

        ocvb.desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        simple.bins = hist.sliders.trackbar(0).Value
        simple.run(src)
        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To src.Channels - 1
            Dim nextEntropy = simple.channelEntropy(src.Total, simple.histNormalized(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
        If standalone Then
            flow.msgs.Add("Entropy total = " + Format(entropy, "0.00") + " - " + entropyChannels)
            flow.Run(ocvb)
        End If
    End Sub
End Class






Public Class Entropy_Highest_MT
    Inherits VBparent
    Dim entropies(0) As Entropy_Simple
    Dim hist As Histogram_Basics
    Public grid As Thread_Grid
    Public bestContrast As cv.Rect
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 80

        hist = New Histogram_Basics(ocvb)

        label1 = "Highest entropy marked with red rectangle"
        ocvb.desc = "Find the highest entropy section of the color image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)

        If entropies.Length <> grid.roiList.Count Then
            ReDim entropies(grid.roiList.Count - 1)
            For i = 0 To entropies.Length - 1
                entropies(i) = New Entropy_Simple()
            Next
        End If

        Dim entropyMap = New cv.Mat(src.Size(), cv.MatType.CV_32F)
        Parallel.For(0, grid.roiList.Count,
         Sub(i)
             entropies(i).Run(src(grid.roiList(i)))
             entropyMap(grid.roiList(i)).SetTo(entropies(i).entropy)
         End Sub)

        Dim maxEntropy As Single = Single.MinValue
        Dim minEntropy As Single = Single.MaxValue
        Dim maxIndex As integer
        For i = 0 To entropies.Count - 1
            If entropies(i).entropy > maxEntropy Then
                maxEntropy = entropies(i).entropy
                maxIndex = i
            End If
            If entropies(i).entropy < minEntropy Then minEntropy = entropies(i).entropy
        Next
        dst2 = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy), minEntropy)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.AddWeighted(dst2, 0.5, src, 0.5, 0, dst2)

        Dim minval As Double, maxval As Double
        Dim tmp = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy))
        cv.Cv2.MinMaxLoc(tmp, minval, maxval)

        dst1 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        bestContrast = grid.roiList(maxIndex)
        If standalone Then dst1.Rectangle(bestContrast, cv.Scalar.Red, 4)
        label2 = "Lighter = higher entropy. Range: " + Format(minEntropy, "0.0") + " to " + Format(maxEntropy, "0.0")
    End Sub
End Class






Public Class Entropy_FAST
    Inherits VBparent
    Dim fast As FAST_Basics
    Dim entropy As Entropy_Highest_MT
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fast = New FAST_Basics(ocvb)
        entropy = New Entropy_Highest_MT(ocvb)

        label1 = "Output of Fast_Basics, input to entropy calculation"
        label2 = "Lighter color is higher entropy, Red marks highest"
        ocvb.desc = "Use FAST markings to add to entropy"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fast.src = src
        fast.Run(ocvb)

        entropy.src = fast.dst1
        entropy.Run(ocvb)
        dst1 = entropy.dst1
        dst2 = entropy.dst2
        dst2.Rectangle(entropy.bestContrast, cv.Scalar.Red, 4)
    End Sub
End Class





' This algorithm is different and does not inherit from ocvbClass.  It is used to reduce the memory load when running MT algorithms above.
Public Class Entropy_Simple
    Public entropy As Single
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public bins As Integer = 256
    Public minRange As Integer = 0
    Public maxRange As Integer = 255
    Public Function channelEntropy(total As Integer, hist As cv.Mat) As Single
        channelEntropy = 0
        For i = 0 To hist.Rows - 1
            Dim hc = Math.Abs(hist.Get(Of Single)(i))
            If hc <> 0 Then channelEntropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return channelEntropy
    End Function
    Public Sub Run(src As cv.Mat)
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To src.Channels - 1
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            histRaw(i) = hist.Clone()
            histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

            Dim nextEntropy = channelEntropy(src.Total, histNormalized(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
    End Sub
End Class