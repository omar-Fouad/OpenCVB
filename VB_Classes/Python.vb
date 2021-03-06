Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

Module Python_Module
    Public Function checkPythonPackage(ocvb As VBocvb, packageName As String) As Boolean
        ' make sure that opencv-python and numpy are installed on this system.
        If ocvb.parms.pythonExe = "" Then
            ocvb.trueText("Python is not present and needs to be installed." + vbCrLf +
                                                  "Get Python 3.7+ with Visual Studio's Install app.")
            Return False
        End If
        Dim pythonFileInfo = New FileInfo(ocvb.parms.pythonExe)
        Dim packageDir = New FileInfo(pythonFileInfo.DirectoryName + "\Lib\site-packages\")
        Dim packageFolder As New IO.DirectoryInfo(packageDir.DirectoryName + "\")
        Dim packageFiles = packageFolder.GetDirectories(packageName, IO.SearchOption.TopDirectoryOnly)

        If packageFiles.Count = 0 Then
            ocvb.trueText("Python is present but the packages needed by this Python script are not present." + vbCrLf +
                                                  "Use the PythonPackages.py script to show which imports are missing.'" + vbCrLf +
                                                  "Go to the Visual Studio menu 'Tools/Python/Python Environments'" + vbCrLf +
                                                  "Select 'Packages' in the combo box and search for packages required by this script.")
        End If
        Return True
    End Function

    Public Function StartPython(ocvb As VBocvb, arguments As String) As Boolean
        If checkPythonPackage(ocvb, "numpy") = False Or checkPythonPackage(ocvb, "cv2") = False Then Return False
        Dim pythonApp = New FileInfo(ocvb.PythonFileName)

        ' when running the regression tests, some python processes are not completing before the next starts.  Then they build up.  What a mess.  This prevents it
        If ocvb.parms.testAllRunning Then
            For Each p In Process.GetProcesses
                If p.ProcessName.ToUpper.Contains("PYTHON") Then
                    Try
                        ' if it is not our process, we won't be able to kill it.
                        p.Kill()
                    Catch ex As Exception
                        Console.WriteLine("Out of sync 'Test All' tried to kill algorithm that was already terminated.")
                    End Try
                End If
            Next
        End If
        If pythonApp.Exists Then
            Dim p As New Process
            p.StartInfo.FileName = ocvb.parms.pythonExe
            p.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            If arguments = "" Then
                p.StartInfo.Arguments = """" + pythonApp.Name + """"
            Else
                p.StartInfo.Arguments = """" + pythonApp.Name + """" + " " + arguments
            End If
            If ocvb.parms.ShowConsoleLog = False Then p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            If p.Start() = False Then MsgBox("The Python script " + pythonApp.Name + " failed to start")
        Else
            ocvb.trueText(pythonApp.FullName + " is missing.")
            Return False
        End If
        Return True
    End Function
End Module





Public Class Python_Run
    Inherits VBparent
    Dim tryCount As integer
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        If ocvb.PythonFileName = "" Then ocvb.PythonFileName = ocvb.parms.homeDir + "VB_Classes/Python/PythonPackages.py"
        Dim pythonApp = New FileInfo(ocvb.PythonFileName)

        If pythonApp.Name.EndsWith("_PS.py") Then
            pyStream = New PyStream_Basics(ocvb)
        Else
            StartPython(ocvb, "")
        End If
        ocvb.desc = "Run Python app: " + pythonApp.Name
        label1 = ""
        label2 = ""
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If pyStream IsNot Nothing Then
            pyStream.src = src
            pyStream.Run(ocvb)
        Else
            Dim proc = Process.GetProcessesByName("python")
            If proc.Count = 0 Then
                If tryCount < 3 Then StartPython(ocvb, "")
                tryCount += 1
            End If
        End If
    End Sub
End Class





Public Class Python_MemMap
    Inherits VBparent
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Public memMapValues(49) As Double ' more than we need - buffer for growth
    Public memMapbufferSize As integer
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        If ocvb.PythonFileName Is Nothing Then
            ocvb.PythonFileName = ocvb.parms.homeDir + "VB_Classes/Python/Python_MemMap.py"
        End If

        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * memMapValues.Length
        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("Python_MemMap", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length - 1)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If standalone Then
            If ocvb.parms.externalPythonInvocation = False Then
                StartPython(ocvb, "--MemMapLength=" + CStr(memMapbufferSize))
            End If
            Dim pythonApp = New FileInfo(ocvb.PythonFileName)
            label1 = "No output for Python_MemMap - see Python console"
            ocvb.desc = "Run Python app: " + pythonApp.Name + " to share memory with OpenCVB and Python."
        End If
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then memMapValues(0) = ocvb.frameCount
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)
    End Sub
End Class





Public Class Python_SurfaceBlit
    Inherits VBparent
    Dim memMap As Python_MemMap
    Dim pipeName As String
    Dim pipe As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim PythonReady As Boolean
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ' this Python script requires pygame to be present...
        If checkPythonPackage(ocvb, "pygame") = False Then
            PythonReady = False
            Exit Sub
        End If
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut)
        PipeTaskIndex += 1

        ' this Python script assumes that fast processing is off - the pointcloud is being used and cannot be resized.
        ocvb.PythonFileName = ocvb.parms.homeDir + "VB_Classes/Python/Python_SurfaceBlit.py"
        memMap = New Python_MemMap(ocvb)

        If ocvb.parms.externalPythonInvocation Then
            PythonReady = True ' python was already running and invoked OpenCVB.
        Else
            PythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If PythonReady Then pipe.WaitForConnection()
        ocvb.desc = "Stream data to Python_SurfaceBlit Python script."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If PythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, src.Total * src.ElemSize, 0, src.Rows, src.Cols)
            Next
            memMap.Run(ocvb)

            Dim rgb = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2RGB)
            If rgbBuffer.Length <> rgb.Total * rgb.ElemSize Then ReDim rgbBuffer(rgb.Total * rgb.ElemSize - 1)
            Marshal.Copy(rgb.Data, rgbBuffer, 0, rgb.Total * rgb.ElemSize)

            If pipe.IsConnected Then
                On Error Resume Next
                pipe.Write(rgbBuffer, 0, rgbBuffer.Length)
            End If
            ocvb.trueText("Blit works fine when run inline but fails with Python callback." + vbCrLf +
                                                  "See 'Python_SurfaceBlit_PS.py' for the surfaceBlit failure")
        Else
            ocvb.trueText("Python is not available")
        End If
    End Sub
End Class
