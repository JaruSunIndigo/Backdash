dotnet build -c Release %~dp0\..\..
pushd %~dp0\..\..\bin\Release\net8.0
del *.log
del logs\*.log
start SpaceWar 9000 4 local 127.0.0.1:9001 127.0.0.1:9002 127.0.0.1:9003
start SpaceWar 9001 4 127.0.0.1:9000 local 127.0.0.1:9002 127.0.0.1:9003
start SpaceWar 9002 4 127.0.0.1:9000 127.0.0.1:9001 local 127.0.0.1:9003
start SpaceWar 9003 4 127.0.0.1:9000 127.0.0.1:9001 127.0.0.1:9002 local
popd
