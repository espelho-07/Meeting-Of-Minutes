$ErrorActionPreference = "Stop"

$siteName = "MeetingOfMinutes"
$publishPath = "D:\Publish\MeetingOfMinutes"
$port = 8080
$appPoolName = "MeetingOfMinutesPool"

Import-Module WebAdministration

if (-not (Test-Path $publishPath)) {
    throw "Publish path not found: $publishPath"
}

if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName | Out-Null
}

Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name processModel.identityType -Value 4

if (Test-Path "IIS:\Sites\$siteName") {
    Stop-Website -Name $siteName -ErrorAction SilentlyContinue
    Remove-Website -Name $siteName
}

New-Website -Name $siteName -PhysicalPath $publishPath -Port $port -ApplicationPool $appPoolName | Out-Null

$uploadPath = Join-Path $publishPath "wwwroot\\uploads"
$profileDocsPath = Join-Path $publishPath "wwwroot\\profile-documents"

foreach ($path in @($publishPath, $uploadPath, $profileDocsPath)) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Force -Path $path | Out-Null
    }

    $acl = Get-Acl $path
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit, ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($rule)
    Set-Acl -Path $path -AclObject $acl
}

Start-Website -Name $siteName

Write-Host ""
Write-Host "IIS site created successfully." -ForegroundColor Green
Write-Host "Site Name : $siteName"
Write-Host "Path      : $publishPath"
Write-Host "URL       : http://localhost:$port"
Write-Host ""
