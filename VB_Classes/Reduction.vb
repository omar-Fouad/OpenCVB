Imports cv = OpenCvSharp
Public Class Reduction_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reduction factor", 1, 255, 64)
        label1 = "Reduced color image."
        desc = "Reduction: a simple way to get KMeans with much less work"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = src / sliders.trackbar(0).Value ' can be any mat type...
        dst1 *= sliders.trackbar(0).Value
    End Sub
End Class







Public Class Reduction_Edges
    Inherits ocvbClass
    Dim edges As Edges_Laplacian
    Dim kReduce As Reduction_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        edges = New Edges_Laplacian(ocvb)
        kReduce = New Reduction_Basics(ocvb)
        label1 = "Reduced image"
        label2 = "Laplacian edges of reduced image"
        desc = "The simplest kmeans is to just reduce the resolution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        kReduce.src = src
        kReduce.Run(ocvb)
        dst1 = kReduce.dst1.Clone

        edges.src = src
        edges.Run(ocvb)
        dst2 = edges.dst1
    End Sub
End Class




Public Class Reduction_Floodfill
    Inherits ocvbClass
    Public bflood As Floodfill_Identifiers
    Public kReduce As Reduction_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        bflood = New Floodfill_Identifiers(ocvb)
        kReduce = New Reduction_Basics(ocvb)
        desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        kReduce.src = src
        kReduce.Run(ocvb)

        bflood.src = kReduce.dst1
        bflood.Run(ocvb)

        dst1 = bflood.dst2
    End Sub
End Class






Public Class Reduction_KNN
    Inherits ocvbClass
    Dim kReduce As Reduction_Basics
    Dim bflood As FloodFill_Black
    Dim pTrack As Kalman_PointTracker
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        bflood = New FloodFill_Black(ocvb)
        kReduce = New Reduction_Basics(ocvb)

        pTrack = New Kalman_PointTracker(ocvb)
        desc = "Use KNN with reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        kReduce.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        kReduce.Run(ocvb)

        bflood.src = kReduce.dst1
        bflood.Run(ocvb)
        dst2 = bflood.dst2

        pTrack.queryPoints = New List(Of cv.Point2f)(bflood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(bflood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(bflood.masks)
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        Dim vw = pTrack.viewObjects
        For i = 0 To vw.Count - 1
            dst1.Circle(vw.Values(i).centroid, 6, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Circle(vw.Values(i).centroid, 4, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class