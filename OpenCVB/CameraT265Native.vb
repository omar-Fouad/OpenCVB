﻿Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module T265_Module_CPP
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Open(w As Int32, h As Int32) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawWidth(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawHeight(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub T265WaitFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsRight(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PoseData(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265IMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Color(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265timeStampLatency(timeStamp As Double) As Single
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub T265InitializeSGM(tp As IntPtr)
    End Sub
End Module

    Structure T265IMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class CameraT265Native
    Inherits Camera
    Dim rawHeight As Int32
    Dim rawWidth As Int32

    Public extrinsics As rs.Extrinsics
    Public pc As New rs.PointCloud
    Dim rawSrcRect As cv.Rect
    Dim rawDstRect As cv.Rect

    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        deviceName = "Intel T265"
        IMU_Present = True
        w = width
        h = height

        cPtr = T265Open(width, height)
        rawWidth = T265RawWidth(cPtr)
        rawHeight = T265RawHeight(cPtr)

        Dim intrin = T265intrinsicsLeft(cPtr)
        Dim intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        intrinsicsLeft_VB = setintrinsics(intrinsicsLeft)

        intrin = T265intrinsicsRight(cPtr)
        Dim intrinsicsRight = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        intrinsicsRight_VB = setintrinsics(intrinsicsRight)

        Dim extrin = T265Extrinsics(cPtr)
        extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation

        rightView = New cv.Mat(h, w, cv.MatType.CV_8U, 0)
        leftView = New cv.Mat(h, w, cv.MatType.CV_8U, 0)

        rawSrcRect = New cv.Rect((rawWidth - rawWidth * h / rawHeight) / 2, 0, rawWidth * h / rawHeight, h)
        rawDstRect = New cv.Rect(0, 0, rawSrcRect.Width, rawSrcRect.Height)

        pointCloud = New cv.Mat()
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Or cPtr = 0 Then Exit Sub

        If frameCount = 0 Then T265InitializeSGM(cPtr)
        T265WaitFrame(cPtr)

        SyncLock OpenCVB.camPic ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
            Dim posePtr = T265PoseData(cPtr)
            Dim pose = Marshal.PtrToStructure(Of PoseData)(posePtr)
            IMU_TimeStamp = T265IMUTimeStamp(cPtr)
            Static imuStartTime = IMU_TimeStamp
            IMU_TimeStamp -= imuStartTime

            IMU_Rotation = pose.rotation
            Dim q = IMU_Rotation
            IMU_Translation = pose.translation
            IMU_Acceleration = pose.acceleration
            IMU_Velocity = pose.velocity
            IMU_AngularAcceleration = pose.angularAcceleration
            IMU_AngularVelocity = pose.angularVelocity
            Dim t = IMU_Translation
            '  Set the matrix as column-major for convenient work with OpenGL and rotate by 180 degress (by negating 1st and 3rd columns)
            Dim mat() As Single = {
                        -(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z), -(2 * q.X * q.Y + 2 * q.Z * q.W), -(2 * q.X * q.Z - 2 * q.Y * q.W), 0.0,
                        2 * q.X * q.Y - 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z + 2 * q.X * q.W, 0.0,
                        -(2 * q.X * q.Z + 2 * q.Y * q.W), -(2 * q.Y * q.Z - 2 * q.X * q.W), -(1 - 2 * q.X * q.X - 2 * q.Y * q.Y), 0.0,
                        t.X, t.Y, t.Z, 1.0}
            transformationMatrix = mat

            leftView = New cv.Mat(h, w, cv.MatType.CV_8UC3, T265Color(cPtr)).Clone()
            color = leftView

            rightView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265RightRaw(cPtr))
            leftView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265LeftRaw(cPtr))

            rightView(rawDstRect) = rightView(rawSrcRect)
            leftView(rawDstRect) = leftView(rawSrcRect)

            ' Console.WriteLine("rgb depth = " + Hex(T265RGBDepth(cPtr).ToInt64))
            RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, T265RGBDepth(cPtr)).Clone()
            depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, T265Depth16(cPtr)).Clone()

            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End SyncLock
    End Sub
End Class
