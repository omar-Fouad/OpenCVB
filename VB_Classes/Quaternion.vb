﻿Imports cv = OpenCvSharp
Imports System.Numerics
Public Class Quaterion_Basics : Implements IDisposable
    Dim sliders1 As New OptionsSliders
    Dim sliders2 As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders1.setupTrackBar1(ocvb, "quaternion A.x X100", -100, 100, 0)
        sliders1.setupTrackBar2(ocvb, "quaternion A.y X100", -100, 100, 0)
        sliders1.setupTrackBar3(ocvb, "quaternion A.z X100", -100, 100, 0)
        sliders1.setupTrackBar4(ocvb, "quaternion Theta X100", -100, 100, 100)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders2.setupTrackBar1(ocvb, "quaternion B.x X100", -100, 100, 0)
        sliders2.setupTrackBar2(ocvb, "quaternion B.y X100", -100, 100, 0)
        sliders2.setupTrackBar3(ocvb, "quaternion B.z X100", -100, 100, 0)
        sliders2.setupTrackBar4(ocvb, "quaternion Theta X100", -100, 100, 100)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        ocvb.desc = "Use the quaternion values to multiply and compute conjugate"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim q1 = New Quaternion(CSng(sliders1.TrackBar1.Value / 100), CSng(sliders1.TrackBar2.Value / 100),
                                CSng(sliders1.TrackBar3.Value / 100), CSng(sliders1.TrackBar4.Value / 100))
        Dim q2 = New Quaternion(CSng(sliders2.TrackBar1.Value / 100), CSng(sliders2.TrackBar2.Value / 100),
                                CSng(sliders2.TrackBar3.Value / 100), CSng(sliders2.TrackBar4.Value / 100))

        Dim quatmul = Quaternion.Multiply(q1, q2)
        ocvb.putText(New ActiveClass.TrueType("q1 = " + q1.ToString(), 10, 60))
        ocvb.putText(New ActiveClass.TrueType("q2 = " + q2.ToString(), 10, 80))
        ocvb.putText(New ActiveClass.TrueType("Multiply q1 * q2" + quatmul.ToString(), 10, 100))

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




' https://github.com/IntelRealSense/librealsense/tree/master/examples/pose-predict
Public Class Quaterion_IMUPrediction : Implements IDisposable
    Dim imu As IMU_FrameTime
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_FrameTime(ocvb)
        imu.plot.sliders.Hide()
        imu.externalUse = True

        ocvb.label1 = "Quaternion_IMUPrediction"
        ocvb.desc = "IMU arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data."
    End Sub
    Private Function quaternion_exp(v As cv.Point3f) As Quaternion
        v *= 0.5
        Dim theta2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z
        Dim theta = Math.Sqrt(theta2)
        Dim c = Math.Cos(theta)
        Dim s = If(theta2 < Math.Sqrt(120 * Single.Epsilon), 1 - theta2 / 6, Math.Sin(theta) / theta2)
        Return New Quaternion(s * v.X, s * v.Y, s * v.Z, c)
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        imu.Run(ocvb)

        Dim dt = 4 'imu.smoothedLatency ' this is the time from IMU measurement to the time the CPU got the pose data.

        Dim t = ocvb.parms.IMU_Translation
        Dim predictedTranslation = New cv.Point3f(dt * (dt / 2 * ocvb.parms.IMU_Acceleration.X + ocvb.parms.IMU_Velocity.X) + t.X,
                                                  dt * (dt / 2 * ocvb.parms.IMU_Acceleration.Y + ocvb.parms.IMU_Velocity.Y) + t.Y,
                                                  dt * (dt / 2 * ocvb.parms.IMU_Acceleration.Z + ocvb.parms.IMU_Velocity.Z) + t.Z)

        Dim predictedW = New cv.Point3f(dt * (dt / 2 * ocvb.parms.IMU_AngularAcceleration.X + ocvb.parms.IMU_AngularVelocity.X),
                                        dt * (dt / 2 * ocvb.parms.IMU_AngularAcceleration.Y + ocvb.parms.IMU_AngularVelocity.Y),
                                        dt * (dt / 2 * ocvb.parms.IMU_AngularAcceleration.Z + ocvb.parms.IMU_AngularVelocity.Z))

        Dim predictedRotation As New Quaternion
        predictedRotation = Quaternion.Multiply(quaternion_exp(predictedW), ocvb.parms.IMU_Rotation)

        Dim diffq = Quaternion.Subtract(ocvb.parms.IMU_Rotation, predictedRotation)

        ocvb.putText(New ActiveClass.TrueType("IMU_Acceleration = " + ocvb.parms.IMU_Acceleration.ToString(), 10, 40))
        ocvb.putText(New ActiveClass.TrueType("IMU_Velocity = " + ocvb.parms.IMU_Velocity.ToString(), 10, 60))
        ocvb.putText(New ActiveClass.TrueType("IMU_AngularAcceleration = " + ocvb.parms.IMU_AngularAcceleration.ToString(), 10, 80))
        ocvb.putText(New ActiveClass.TrueType("IMU_AngularVelocity = " + ocvb.parms.IMU_AngularVelocity.ToString(), 10, 100))
        ocvb.putText(New ActiveClass.TrueType("dt = " + dt.ToString(), 10, 120))
        ocvb.putText(New ActiveClass.TrueType("Pose quaternion = " + ocvb.parms.IMU_Rotation.ToString(), 10, 140))
        ocvb.putText(New ActiveClass.TrueType("Prediction Rotation = " + predictedRotation.ToString(), 10, 160))
        ocvb.putText(New ActiveClass.TrueType("difference = " + diffq.ToString(), 10, 180))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
    End Sub
End Class