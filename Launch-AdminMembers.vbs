Set WshShell = CreateObject("WScript.Shell")
Set objFSO = CreateObject("Scripting.FileSystemObject")

' Get the directory where this script is located
strScriptPath = objFSO.GetParentFolderName(WScript.ScriptFullName)

' Change to the script directory
WshShell.CurrentDirectory = strScriptPath

' Show a message box that we're starting
MsgBox "Starting AdminMembers Application..." & vbCrLf & vbCrLf & _
       "The application will launch in a moment." & vbCrLf & _
       "A browser window will open automatically.", _
       vbInformation, "AdminMembers Launcher"

' Start the application in a new command window
WshShell.Run "cmd /c start ""AdminMembers Server"" cmd /k ""echo ====================================== & echo AdminMembers Server Running & echo ====================================== & echo. & echo Press Ctrl+C to stop the server & echo. & echo Starting server... & echo. & dotnet run""", 1, False

' Wait for server to start
WScript.Sleep 8000

' Open the browser
WshShell.Run "https://localhost:7223/index.html", 1, False

' Show success message
MsgBox "AdminMembers application has been launched!" & vbCrLf & vbCrLf & _
       "The application should now be open in your browser." & vbCrLf & vbCrLf & _
       "URL: https://localhost:7223/index.html" & vbCrLf & vbCrLf & _
       "To stop the server, close the 'AdminMembers Server' window.", _
       vbInformation, "AdminMembers Launcher"
