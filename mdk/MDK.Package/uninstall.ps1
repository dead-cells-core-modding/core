
[System.Environment]::SetEnvironmentVariable("DCCM_MDK_BIN_ROOT",  $null, "User")
[System.Environment]::SetEnvironmentVariable("DEAD_CELLS_GAME_PATH", $null, "User")
[System.Environment]::SetEnvironmentVariable("DCCM_MDK_ROOT",  $null, "User")
dotnet nuget remove source DeadCoreModdingMDK
