﻿Imports cv = OpenCvSharp

Public Class VBocvb
    ' all the items here are used to communicate to/from the host user interface.  Other variables common to all algorithms should be ocvbClass.vb
    Public color As cv.Mat
    Public RGBDepth As cv.Mat
    Public result As New cv.Mat
    Public pointCloud As cv.Mat
    Public depth16 As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public frameCount As Integer = 0
    Public label1 As String
    Public label2 As String
    Public quadrantIndex As Integer = 0
    Public parms As ActiveTask.algParms

    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.

    Public PythonFileName As String
    Public TTtextData As List(Of TTtext)

    Public algorithmIndex As Integer
    Public parentRoot As String
    Public parentAlgorithm As String
    Public callTrace As New List(Of String)

    Public transformationMatrix() As Single
    Public fixedColors(255) As cv.Scalar

    Public openFileDialogRequested As Boolean
    Public openFileInitialDirectory As String
    Public openFileFilter As String
    Public openFileFilterIndex As Integer
    Public openFileDialogName As String
    Public openFileDialogTitle As String
    Public openFileSliderPercent As Single
    Public fileStarted As Boolean
    Public initialStartSetting As Boolean

    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public IMU_Translation As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_Velocity As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b
    Public desc As String
    Public Sub New(resolution As cv.Size, parms As ActiveTask.algParms, location As cv.Rect)
        color = New cv.Mat(resolution.Height, resolution.Width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(color.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result = New cv.Mat(color.Height, color.Width * 2, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        TTtextData = New List(Of TTtext)
    End Sub
    Public Sub trueText(text As String, Optional x As Integer = 10, Optional y As Integer = 40, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, x, y, picTag)
        TTtextData.Add(str)
    End Sub
    Public Sub trueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, pt.X, pt.Y, picTag)
        TTtextData.Add(str)
    End Sub
End Class
