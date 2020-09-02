Imports cv = OpenCvSharp
Public Class Covariance_Basics
    Inherits ocvbClass
    Dim random As Random_Points
    Public samples As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        random = New Random_Points(ocvb)
        desc = "Calculate the covariance of random depth data points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim covariance As New cv.Mat, mean = New cv.Mat
        dst2.SetTo(0)
        If standalone Then
            random.Run(ocvb)
            samples = New cv.Mat(random.Points.Length, 2, cv.MatType.CV_32F, random.Points2f)
            For i = 0 To random.Points.Length - 1
                dst2.Circle(random.Points(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            Next
        End If
        Dim samples2 = samples.Reshape(2)
        cv.Cv2.CalcCovarMatrix(samples, covariance, mean, cv.CovarFlags.Cols)
        Dim overallMean = samples2.Mean()
        ocvb.trueText(New TTtext("Covar(0, 0), Covar(0, 1)" + vbTab + Format(covariance.Get(Of Double)(0, 0), "#0.0") + vbTab +
                     Format(covariance.Get(Of Double)(0, 1), "#0.0"), 20, 60))
        ocvb.trueText(New TTtext("Covar(1 0), Covar(1, 1)" + vbTab + Format(covariance.Get(Of Double)(1, 0), "#0.0") + vbTab +
                     Format(covariance.Get(Of Double)(1, 1), "#0.0"), 20, 90))
        ocvb.trueText(New TTtext("Mean X, Mean Y" + vbTab + vbTab + Format(overallMean(0), "#0.00") + vbTab + vbTab +
                     Format(overallMean(1), "#0.00"), 20, 120))
        If standalone Then
            Dim newCenter = New cv.Point(overallMean(0), overallMean(1))
            Static lastCenter = newCenter
            dst2.Circle(newCenter, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst2.Circle(lastCenter, 5, cv.Scalar.Yellow, 2, cv.LineTypes.AntiAlias)
            dst2.Line(newCenter, lastCenter, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
            lastCenter = newCenter
            ocvb.trueText(New TTtext("Yellow is last center, red is the current center", 20, 150))
        End If
    End Sub
End Class



' http://answers.opencv.org/question/31228/how-to-use-function-calccovarmatrix/
Public Class Covariance_Test
    Inherits ocvbClass
    Dim covar As Covariance_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        covar = New Covariance_Basics(ocvb)
        desc = "Test the covariance basics algorithm."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim testInput() As Double = {1.5, 2.3, 3.0, 1.7, 1.2, 2.9, 2.1, 2.2, 3.1, 3.1, 1.3, 2.7, 2.0, 1.7, 1.0, 2.0, 0.5, 0.6, 1.0, 0.9}
        covar.samples = New cv.Mat(10, 2, cv.MatType.CV_64F, testInput)
        covar.Run(ocvb)
        ocvb.trueText(New TTtext("Results should be a symmetric array with 2.1 and -2.1", 20, 150))
    End Sub
End Class

