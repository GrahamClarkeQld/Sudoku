param([string]$ProjectDir, [string]$ProjectPath);

$currentDate = get-date -format dd.MM.yyyy.HHmm;
$find = "<Version>(.|\n)*?</Version>";
$replace = "<Version>" + $currentDate + "</Version>";
$csproj = Get-Content $ProjectPath
$csprojUpdated = $csproj -replace $find, $replace

Set-Content -Path $ProjectPath -Value $csprojUpdated

  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="powershell.exe -ExecutionPolicy Unrestricted -NoProfile -NonInteractive -File $(ProjectDir)\post-build.ps1 -ProjectDir $(ProjectDir) -ProjectPath $(ProjectPath)" />
  </Target>
