Imports cv = OpenCvSharp
Public Class Extrinsics_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        setDescription(ocvb, "Show the depth camera extrinsics.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim ttStart = 40
        ocvb.trueText(New TTtext("Rotation Matrix                                             Translation", 10, ttStart))
        Dim fmt = "#0.0000"
        Dim nextLine = Format(ocvb.parms.extrinsics.rotation(0), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(1), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(2), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(0), fmt)
        ocvb.trueText(New TTtext(nextLine, 10, ttStart + 20))

        nextLine = Format(ocvb.parms.extrinsics.rotation(3), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(4), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(5), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(1), fmt)
        ocvb.trueText(New TTtext(nextLine, 10, ttStart + 40))

        nextLine = Format(ocvb.parms.extrinsics.rotation(6), fmt) + vbTab + Format(ocvb.parms.extrinsics.rotation(7), fmt) + vbTab +
                       Format(ocvb.parms.extrinsics.rotation(8), fmt) + vbTab + vbTab + vbTab + Format(ocvb.parms.extrinsics.translation(2), fmt)
        ocvb.trueText(New TTtext(nextLine, 10, ttStart + 60))
    End Sub
End Class

