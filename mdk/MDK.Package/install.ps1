
[System.Environment]::SetEnvironmentVariable("DCCM_MDK_ROOT", "$PSScriptRoot", "User")
dotnet nuget add source "$PSScriptRoot\packages"  --name DeadCoreModdingMDK

