#!/usr/bin/env pwsh
#Requires -Version 6.2
$ErrorActionPreference = "Stop"

$AwsCliPath = "/usr/local/bin/aws"
$WorldPath = "/home/filhit/terraria/Worlds/EdgeOfSacrilege.wld"
$S3Bucket = "tyshkavets-terraria-backups"

$BackupPath = "$WorldPath.bak"
$LatestBackupPath = "$WorldPath.latest.bak"
$Threshhold = New-TimeSpan -Minutes 10

if (-not (Test-Path $WorldPath)) {
    Write-Output "No world at $WorldPath."
    Exit
}

if (-not (Test-Path $BackupPath)) {
    Write-Output "No backup at $BackupPath."
    Exit
}

$Backup = Get-Item $BackupPath
if (($Backup.LastWriteTime + $Threshhold) -gt (Get-Date)) {
    Write-Output "Too soon. $BackupPath was written $((Get-Date) - $Backup.LastWriteTime) ago."
    Exit
}

if (Test-Path $LatestBackupPath) {
    $LatestBackup = Get-Item $LatestBackupPath
    $Date = "{0:yyyy-MMM-dd}" -f $LatestBackup.LastWriteTime
    $UniqueBackupPath = "$WorldPath.$Date.bak"
    $i = 1;
    while (Test-Path $UniqueBackupPath) {
        $i += 1
        $UniqueBackupPath = "$WorldPath.$Date.$i.bak"
    }

    Write-Output "$LatestBackupPath exists. Moving to $UniqueBackupPath"
    Move-Item $LatestBackupPath $UniqueBackupPath
}

Write-Output "Moving $BackupPath to $LatestBackupPath"
Move-Item $BackupPath $LatestBackupPath
Write-Output "Uploading to s3"
$WorldFileName = Split-Path -Leaf $WorldPath
& $AwsCliPath s3 cp $LatestBackupPath s3://$S3Bucket/$WorldFileName
