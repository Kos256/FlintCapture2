@echo off

echo This will leave the build in the 'bin' directory

dotnet publish FlintCapture2.csproj ^
    -c RuntimeExcluded ^
    -r win-x64 ^
    --self-contained false ^
    -p:PublishDir=bin\RuntimeExcluded\

move /Y "bin\RuntimeExcluded\FlintCapture2.exe" "bin\[RE] FlintCapture.exe"
rmdir /S /Q "bin\RuntimeExcluded"
pause