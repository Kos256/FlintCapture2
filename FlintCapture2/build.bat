echo "this will leave the build in the 'bin' directory" 
dotnet publish FlintCapture2.csproj -c Release -r win-x64 --self-contained true
move "bin\Release\net10.0-windows\win-x64\publish\FlintCapture2.exe" "bin\FlintCapture.exe"

pause