﻿Imports cv = OpenCvSharp
Imports System.Threading

Module Puzzle_Solvers
    Public Enum tileSide
        top
        bottom
        left
        right
        none
    End Enum
    Public Enum cornerType
        upperLeft
        upperRight
        lowerLeft
        lowerRight
        none
    End Enum
    Public Structure bestFit
        Public index As Int32
        Public bestMetricUp As Single
        Public bestMetricDn As Single
        Public bestMetricLt As Single
        Public bestMetricRt As Single
        Public bestUp As List(Of Integer)
        Public bestDn As List(Of Integer)
        Public bestLt As List(Of Integer)
        Public bestRt As List(Of Integer)
        Public AvgMetric As Single
        Public MinBest As Single
        Public maxBest As Single
        Public maxBestIndex As Integer
        Public edge As tileSide
        Public corner As cornerType
    End Structure
    Public Class fit
        Public index As Int32
        Public neighbor As Integer
        Public metricUp As Single
        Public metricDn As Single
        Public metricLt As Single
        Public metricRt As Single
        Sub New(abs() As Single, _index As Int32, n As Int32)
            metricUp = abs(0)
            metricDn = abs(1)
            metricLt = abs(2)
            metricRt = abs(3)
            index = _index
            neighbor = n
        End Sub
    End Class
    Public Class CompareSingle : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Private Function computeMetric(sample() As cv.Mat) As Single()
        Dim tmp As New cv.Mat
        Dim absDiff(4 - 1) As Single
        For i = 0 To 8 - 1 Step 2
            cv.Cv2.Absdiff(sample(i), sample(i + 1), tmp)
            Dim absD = cv.Cv2.Sum(tmp)
            For j = 0 To 3 - 1
                If absDiff(i / 2) < absD.Item(j) Then absDiff(i / 2) = absD.Item(j)
            Next
        Next
        Return absDiff
    End Function
    Public Function getEdgeType(fit As bestFit) As tileSide
        Dim edge As tileSide
        For i = 0 To 4 - 1
            Dim nextBest = Choose(i + 1, fit.bestMetricUp, fit.bestMetricDn, fit.bestMetricLt, fit.bestMetricRt)
            If nextBest = fit.MinBest Then
                edge = Choose(i + 1, tileSide.top, tileSide.bottom, tileSide.left, tileSide.right)
                Exit For
            End If
        Next
        Return edge
    End Function
    Public Function getCornerType(ByRef fit As bestFit) As cornerType
        Dim sortBest As New SortedList(Of Single, Single)(New CompareSingle)
        For i = 0 To 4 - 1
            Dim nextVal = Choose(i + 1, fit.bestMetricUp, fit.bestMetricDn, fit.bestMetricLt, fit.bestMetricRt)
            Dim nextIndex = Choose(i + 1, 1, 2, 3, 4)
            sortBest.Add(nextVal, nextIndex)
        Next

        If sortBest.ElementAt(2).Value = 1 And sortBest.ElementAt(3).Value = 3 Or sortBest.ElementAt(2).Value = 3 And sortBest.ElementAt(3).Value = 1 Then
            Return cornerType.upperLeft
        End If

        If sortBest.ElementAt(2).Value = 1 And sortBest.ElementAt(3).Value = 4 Or sortBest.ElementAt(2).Value = 4 And sortBest.ElementAt(3).Value = 1 Then
            Return cornerType.upperRight
        End If

        If sortBest.ElementAt(2).Value = 2 And sortBest.ElementAt(3).Value = 3 Or sortBest.ElementAt(2).Value = 3 And sortBest.ElementAt(3).Value = 2 Then
            Return cornerType.lowerLeft
        End If

        If sortBest.ElementAt(2).Value = 2 And sortBest.ElementAt(3).Value = 4 Or sortBest.ElementAt(2).Value = 4 And sortBest.ElementAt(3).Value = 2 Then
            Return cornerType.lowerRight
        End If
        Return cornerType.none
    End Function
    Public Function fitCheck(ocvb As AlgorithmData, roilist() As cv.Rect, ByRef fitlist As List(Of bestFit)) As List(Of bestFit)
        Dim saveOptions = ocvb.parms.ShowOptions
        ocvb.parms.ShowOptions = False
        ocvb.parms.ShowOptions = saveOptions

        ' compute absDiff of every top/bottom to every left/right side
        Dim sample(8 - 1) As cv.Mat
        Dim corners As New List(Of bestFit)
        Dim edges As New List(Of Integer)
        Dim sortedCorners As New SortedList(Of Single, bestFit)(New CompareSingle)
        Dim sortedEdges As New SortedList(Of Single, bestFit)(New CompareSingle)
        For roiIndex = 0 To roilist.Count - 1
            Dim maxDiff() = {Single.MinValue, Single.MinValue, Single.MinValue, Single.MinValue}
            Dim roi1 = roilist(roiIndex)
            sample(0) = ocvb.result1(roi1).Row(0)
            sample(2) = ocvb.result1(roi1).Row(roi1.Height - 1)
            sample(4) = ocvb.result1(roi1).Col(0)
            sample(6) = ocvb.result1(roi1).Col(roi1.Width - 1)
            Dim nextFitList As New List(Of fit)
            For j = 0 To roilist.Count - 1
                If roiIndex = j Then Continue For
                Dim roi2 = roilist(j)
                sample(1) = ocvb.result1(roi2).Row(roi1.Height - 1)
                sample(3) = ocvb.result1(roi2).Row(0)
                sample(5) = ocvb.result1(roi2).Col(roi2.Width - 1)
                sample(7) = ocvb.result1(roi2).Col(0)

                Dim absDiff() = computeMetric(sample)
                For k = 0 To maxDiff.Count - 1
                    If maxDiff(k) < absDiff(k) Then maxDiff(k) = absDiff(k)
                Next

                nextFitList.Add(New fit(absDiff, roiIndex, j))
            Next

            Dim sortedUp As New SortedList(Of Single, fit)(New CompareSingle)
            Dim sortedDn As New SortedList(Of Single, fit)(New CompareSingle)
            Dim sortedLt As New SortedList(Of Single, fit)(New CompareSingle)
            Dim sortedRt As New SortedList(Of Single, fit)(New CompareSingle)
            For j = 0 To nextFitList.Count - 1
                nextFitList.ElementAt(j).metricUp = (maxDiff(0) - nextFitList.ElementAt(j).metricUp) / maxDiff(0)
                nextFitList.ElementAt(j).metricDn = (maxDiff(1) - nextFitList.ElementAt(j).metricDn) / maxDiff(1)
                nextFitList.ElementAt(j).metricLt = (maxDiff(2) - nextFitList.ElementAt(j).metricLt) / maxDiff(2)
                nextFitList.ElementAt(j).metricRt = (maxDiff(3) - nextFitList.ElementAt(j).metricRt) / maxDiff(3)
                sortedUp.Add(nextFitList.ElementAt(j).metricUp, nextFitList.ElementAt(j))
                sortedDn.Add(nextFitList.ElementAt(j).metricDn, nextFitList.ElementAt(j))
                sortedLt.Add(nextFitList.ElementAt(j).metricLt, nextFitList.ElementAt(j))
                sortedRt.Add(nextFitList.ElementAt(j).metricRt, nextFitList.ElementAt(j))
            Next
            Dim bestUp As New List(Of Integer)
            Dim bestDn As New List(Of Integer)
            Dim bestLt As New List(Of Integer)
            Dim bestRt As New List(Of Integer)
            For i = 0 To sortedUp.Count - 1
                bestUp.Add(sortedUp.ElementAt(i).Value.neighbor)
                bestDn.Add(sortedDn.ElementAt(i).Value.neighbor)
                bestLt.Add(sortedLt.ElementAt(i).Value.neighbor)
                bestRt.Add(sortedRt.ElementAt(i).Value.neighbor)
            Next
            Dim bestMetricUp = sortedUp.ElementAt(0).Value.metricUp
            Dim bestMetricDn = sortedDn.ElementAt(0).Value.metricDn
            Dim bestMetricLt = sortedLt.ElementAt(0).Value.metricLt
            Dim bestMetricRt = sortedRt.ElementAt(0).Value.metricRt

            Dim Fit As New bestFit
            Fit.AvgMetric = (bestMetricUp + bestMetricDn + bestMetricLt + bestMetricRt) / 4
            Fit.bestUp = bestUp
            Fit.bestDn = bestDn
            Fit.bestLt = bestLt
            Fit.bestRt = bestRt
            Fit.bestMetricUp = bestMetricUp
            Fit.bestMetricDn = bestMetricDn
            Fit.bestMetricLt = bestMetricLt
            Fit.bestMetricRt = bestMetricRt
            Fit.edge = tileSide.none
            Fit.corner = cornerType.none
            Dim minVal As Single = Single.MaxValue, maxVal As Single = Single.MinValue
            Dim maxBestIndex = 0
            For i = 0 To 4 - 1
                Dim nextBest = Choose(i + 1, bestMetricUp, bestMetricDn, bestMetricLt, bestMetricRt)
                If nextBest < minVal Then minVal = nextBest
                If nextBest > maxVal Then
                    maxVal = nextBest
                    maxBestIndex = Choose(i + 1, bestUp.ElementAt(0), bestDn.ElementAt(0), bestLt.ElementAt(0), bestRt.ElementAt(0))
                End If
            Next
            Fit.MinBest = minVal
            Fit.maxBest = maxVal
            Fit.maxBestIndex = maxBestIndex
            Fit.index = roiIndex

            fitlist.Add(Fit)
            Dim belowAvg As Integer = 0
            For j = 0 To 4 - 1
                Dim nextBest = Choose(j + 1, fit.bestMetricUp, fit.bestMetricDn, fit.bestMetricLt, fit.bestMetricRt)
                If fit.AvgMetric > nextBest Then
                    belowAvg += 1
                End If
            Next
            If belowAvg = 2 Then
                fit.corner = getCornerType(fit)
                sortedCorners.Add(fit.maxBest - fit.MinBest, fit)
            End If
            If belowAvg = 1 Then
                fit.edge = getEdgeType(fit)
                sortedEdges.Add(fit.maxBest - fit.MinBest, fit)
            End If
        Next

        For i = 0 To sortedCorners.Count - 1
            corners.Add(sortedCorners.ElementAt(i).Value)
        Next
        Return corners
    End Function
End Module





' https://github.com/nemanja-m/gaps
Public Class Puzzle_Basics : Implements IDisposable
    Public grid As Thread_Grid
    Public scrambled As New List(Of cv.Rect) ' this is every roi regardless of size. 
    Public unscrambled As New List(Of cv.Rect) ' this is every roi regardless of size. 
    Public restartRequested As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = ocvb.color.Width / 10
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 8
        grid.Run(ocvb)
        grid.sliders.Hide()
        ocvb.desc = "Create the puzzle pieces for a genetic matching algorithm."
    End Sub
    Function Shuffle(Of T)(collection As IEnumerable(Of T)) As List(Of T)
        Dim r As Random = New Random()
        Shuffle = collection.OrderBy(Function(a) r.Next()).ToList()
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        Static width As Int32
        Static height As Int32
        If width <> grid.sliders.TrackBar1.Value Or height <> grid.sliders.TrackBar2.Value Or ocvb.frameCount = 0 Or restartRequested Then
            restartRequested = False
            grid.Run(ocvb)
            width = grid.roiList(0).Width
            height = grid.roiList(0).Height

            unscrambled.Clear()
            Dim inputROI As New List(Of cv.Rect)
            For j = 0 To grid.roiList.Count - 1
                Dim roi = grid.roiList(j)
                If roi.Width = width And roi.Height = height Then
                    inputROI.Add(grid.roiList(j))
                    unscrambled.Add(grid.roiList(j))
                End If
            Next

            scrambled = Shuffle(inputROI)
        End If

        ' display image with shuffled roi's
        ocvb.result1.SetTo(0)
        For i = 0 To scrambled.Count - 1
            Dim roi = grid.roiList(i)
            Dim roi2 = scrambled(i)
            If roi.Width = width And roi.Height = height And roi2.Width = width And roi2.Height = height Then ocvb.result1(roi2) = ocvb.color(roi)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class





Public Class Puzzle_Solver : Implements IDisposable
    Dim puzzle As Puzzle_Basics
    Public roilist() As cv.Rect
    Public externalUse As Boolean
    Dim radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        puzzle = New Puzzle_Basics(ocvb)

        radio.Setup(ocvb, 3)
        radio.check(0).Text = "Easy Puzzle - tiles = 256x180"
        radio.check(1).Text = "Medium Puzzle - tiles = 128x90"
        radio.check(2).Text = "Hard Puzzle - tiles = 64x45"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        ocvb.desc = "Put the puzzle back together using the absDiff of the up, down, left and right sides of each ROI."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            If radio.check(0).Checked Then
                puzzle.grid.sliders.TrackBar1.Value = 256
                puzzle.grid.sliders.TrackBar2.Value = 180
            ElseIf radio.check(1).Checked Then
                puzzle.grid.sliders.TrackBar1.Value = 128
                puzzle.grid.sliders.TrackBar2.Value = 90
            Else
                puzzle.grid.sliders.TrackBar1.Value = 64
                puzzle.grid.sliders.TrackBar2.Value = 45
            End If
            puzzle.restartRequested = True
            puzzle.Run(ocvb)
            roilist = puzzle.grid.roiList.ToArray
        End If

        Dim fitlist As New List(Of bestFit)
        Dim edgeTotal = CInt(ocvb.color.Height / roilist(0).Height)
        Dim cornerlist = fitCheck(ocvb, roilist, fitlist)
        Dim bestCorner = cornerlist.ElementAt(0)
        Dim fit = bestCorner
        Dim roi = roilist(fit.index)
        Dim startcorner = bestCorner.corner
        Select Case bestCorner.corner
            Case cornerType.upperLeft, cornerType.upperRight
                For nexty = 0 To ocvb.result2.Height - 1 Step roi.Height
                    For nextx = 0 To ocvb.result2.Width - 1 Step roi.Width
                        ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextx, nexty, roi.Width, roi.Height)))
                        If startcorner = cornerType.upperLeft Then
                            fit = fitlist.ElementAt(fit.bestRt.ElementAt(0))
                        Else
                            fit = fitlist.ElementAt(fit.bestLt.ElementAt(0))
                        End If
                        roi = roilist(fit.index)
                    Next
                    fit = fitlist.ElementAt(bestCorner.bestDn.ElementAt(0))
                    roi = roilist(fit.index)
                    bestCorner = fit
                Next
            Case cornerType.lowerLeft, cornerType.lowerRight
                For nexty = ocvb.result2.Height - roi.Height To 0 Step -roi.Height
                    For nextx = 0 To ocvb.result2.Width - 1 Step roi.Width
                        ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextx, nexty, roi.Width, roi.Height)))
                        If startcorner = cornerType.lowerLeft Then
                            fit = fitlist.ElementAt(fit.bestRt.ElementAt(0))
                        Else
                            fit = fitlist.ElementAt(fit.bestLt.ElementAt(0))
                        End If
                        roi = roilist(fit.index)
                    Next
                    fit = fitlist.ElementAt(bestCorner.bestUp.ElementAt(0))
                    roi = roilist(fit.index)
                    bestCorner = fit
                Next
        End Select

        ocvb.label1 = "Input to puzzle solver"
        ocvb.label2 = "Puzzle_Solver output (ambiguities possible)"
        Thread.Sleep(200)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        puzzle.Dispose()
        radio.Dispose()
    End Sub
End Class

