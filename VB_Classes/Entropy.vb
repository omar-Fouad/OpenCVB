﻿Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics : Implements IDisposable
    Dim flow As Font_FlowText
    Dim hist As Histogram_Basics
    Public src As cv.Mat
    Public externalUse As Boolean
    Public entropy As Single
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT1

        hist = New Histogram_Basics(ocvb)
        hist.externalUse = True

        ocvb.desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Private Function channelEntropy(total As Int32, hist As cv.Mat) As Single
        Dim entropy As Single
        For i = 0 To hist.Rows - 1
            Dim hc = Math.Abs(hist.At(Of Single)(i))
            If hc <> 0 Then entropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return entropy
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then src = ocvb.color
        hist.src = src
        hist.Run(ocvb)
        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To 2
            Dim nextEntropy = channelEntropy(src.Total, hist.histRGB(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
        If externalUse = False Then
            flow.msgs.Add("Entropy total = " + Format(entropy, "0.00") + " - " + entropyChannels)
            flow.Run(ocvb)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        hist.Dispose()
        flow.Dispose()
    End Sub
End Class