param(
  [Parameter(Mandatory = $true)]
  [string] $PayloadRoot,

  [Parameter(Mandatory = $true)]
  [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PayloadRoot).Path
$files = @(Get-ChildItem -LiteralPath $root -File -Recurse | Sort-Object FullName)
if ($files.Count -eq 0) { throw "No files found in $root" }

# MSI component GUIDs and IDs must remain stable across releases.
function Get-Key([string] $value)
{
  $sha = [System.Security.Cryptography.SHA256]::Create()
  try
  {
    $bytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value.ToLowerInvariant()))
  }
  finally { $sha.Dispose() }

  $guidBytes = New-Object byte[] 16
  [Array]::Copy($bytes, $guidBytes, 16)
  $guidBytes[7] = [byte](($guidBytes[7] -band 0x0F) -bor 0x50)
  $guidBytes[8] = [byte](($guidBytes[8] -band 0x3F) -bor 0x80)
  return @{
    Id = [Convert]::ToHexString($bytes).Substring(0, 20)
    Guid = ([Guid]::new($guidBytes)).ToString('D')
  }
}

function Escape-Xml([string] $value)
{
  return [System.Security.SecurityElement]::Escape($value)
}

# The installer harvests exactly the already-assembled portable package.
# One stable component + HKCU keypath per file keeps per-user MSI ICE38 compliant.
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$sb.AppendLine('  <Fragment>')
[void]$sb.AppendLine('    <ComponentGroup Id="PublishedFiles">')

$directoryPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($file in $files)
{
  $relative = $file.FullName.Substring($root.Length).TrimStart('\', '/')
  $folder = [System.IO.Path]::GetDirectoryName($relative)
  if (-not [string]::IsNullOrEmpty($folder)) { [void]$directoryPaths.Add($folder) }
}

# Emit nested directory tree later; Component/@Directory uses stable generated folder IDs.
$dirIds = @{}
foreach ($folder in $directoryPaths)
{
  $partial = ''
  foreach ($segment in ($folder -split '[\\/]'))
  {
    $partial = if ($partial) { "$partial\$segment" } else { $segment }
    if (-not $dirIds.ContainsKey($partial))
    {
      $dirIds[$partial] = 'Dir_' + (Get-Key ('directory:' + $partial)).Id
    }
  }
}

$firstInDirectory = @{}
foreach ($file in $files)
{
  $relative = $file.FullName.Substring($root.Length).TrimStart('\', '/')
  $folder = [System.IO.Path]::GetDirectoryName($relative)
  $dir = if ([string]::IsNullOrEmpty($folder)) { 'INSTALLFOLDER' } else { $dirIds[$folder] }
  $key = Get-Key ('file:' + $relative)
  $name = Escape-Xml($file.FullName)
  [void]$sb.AppendLine(('      <Component Id="File_{0}" Directory="{1}" Guid="{{{2}}}">' -f $key.Id, $dir, $key.Guid))
  [void]$sb.AppendLine(('        <File Id="Payload_{0}" Source="{1}" />' -f $key.Id, $name))
  [void]$sb.AppendLine(('        <RegistryValue Root="HKCU" Key="Software\StarBridger\Installer\Components" Name="{0}" Type="integer" Value="1" KeyPath="yes" />' -f $key.Id))
  if (-not $firstInDirectory.ContainsKey($dir))
  {
    $firstInDirectory[$dir] = $true
    [void]$sb.AppendLine(('        <RemoveFolder Id="Remove_{0}" Directory="{1}" On="uninstall" />' -f (Get-Key ('remove:' + $dir)).Id, $dir))
  }
  [void]$sb.AppendLine('      </Component>')
}
[void]$sb.AppendLine('    </ComponentGroup>')
[void]$sb.AppendLine('  </Fragment>')

if ($directoryPaths.Count -gt 0)
{
  [void]$sb.AppendLine('  <Fragment>')
  [void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
  $tree = @{}
  foreach ($folder in $directoryPaths)
  {
    $segments = $folder -split '[\\/]'
    $path = ''
    $parent = ''
    foreach ($segment in $segments)
    {
      $path = if ($path) { "$path\$segment" } else { $segment }
      if (-not $tree.ContainsKey($path))
      {
        $tree[$path] = @{ Parent = $parent; Name = $segment }
      }
      $parent = $path
    }
  }
  function Write-Children([string] $parent, [int] $indent)
  {
    foreach ($key in @($tree.Keys | Where-Object { $tree[$_].Parent -eq $parent } | Sort-Object))
    {
      $spaces = ' ' * $indent
      [void]$sb.AppendLine(($spaces + '<Directory Id="' + $dirIds[$key] + '" Name="' + (Escape-Xml($tree[$key].Name)) + '">'))
      Write-Children $key ($indent + 2)
      [void]$sb.AppendLine(($spaces + '</Directory>'))
    }
  }
  Write-Children '' 6
  [void]$sb.AppendLine('    </DirectoryRef>')
  [void]$sb.AppendLine('  </Fragment>')
}

[void]$sb.AppendLine('</Wix>')
$parentDir = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($OutputPath))
[void][System.IO.Directory]::CreateDirectory($parentDir)
[System.IO.File]::WriteAllText($OutputPath, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Generated WiX components for $($files.Count) file(s): $OutputPath"
