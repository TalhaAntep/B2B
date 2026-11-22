$ErrorActionPreference = 'Stop'
$api = 'http://127.0.0.1:5080'
$server = '(localdb)\MSSQLLocalDB'
$database = 'Store5_Settings_Test'
$member = 'store5'
$cfg = Get-Content -LiteralPath (Join-Path $env:LOCALAPPDATA 'B2BStore\settings.local.json') -Raw | ConvertFrom-Json
$token = [string]$cfg.B2BStore.AdminToken
if ([string]::IsNullOrWhiteSpace($token) -or [string]$cfg.B2BStore.MemberCode -cne $member) { throw 'Local auth configuration is invalid.' }
$headers = @{ Authorization = "Bearer $token" }
function Db([string]$query) {
  $out = @(& sqlcmd -S $server -d $database -E -b -h -1 -W -Q $query 2>$null)
  if ($LASTEXITCODE -ne 0) { throw 'Scoped SQL operation failed.' }
  return (($out -join '').Trim())
}
function AssertTarget {
  $query = "SET NOCOUNT ON; IF DB_NAME()<>N'Store5_Settings_Test' THROW 53300,'db',1; IF (SELECT COUNT_BIG(*) FROM dbo.DealerSettings WHERE MemberCode=N'store5')<>1 THROW 53301,'row',1; IF NOT EXISTS(SELECT 1 FROM dbo.DealerSettings WHERE MemberCode=N'store5' AND ProductCompany IS NULL) THROW 53302,'null',1; SELECT 'OK';"
  $result = Db $query
  if ($result -ne 'OK') { throw 'Target row check failed.' }
}
function Backup([string]$label) {
  AssertTarget
  $dir = Join-Path $env:LOCALAPPDATA 'B2BStore\backups'
  New-Item -ItemType Directory -Path $dir -Force | Out-Null
  $path = [IO.Path]::GetFullPath((Join-Path $dir ("Store5_Settings_Test_{0}_{1}.bak" -f $label,(Get-Date -Format 'yyyyMMdd-HHmmss-ffff'))))
  $root = (Resolve-Path (Get-Location).Path).Path.TrimEnd('\') + '\'
  if ($path.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) { throw 'Backup path must be outside repository.' }
  $escaped = $path.Replace("'","''")
  $query = "SET NOCOUNT ON; BACKUP DATABASE [Store5_Settings_Test] TO DISK=N'$escaped' WITH COPY_ONLY,CHECKSUM,INIT; RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM;"
  $null = & sqlcmd -S $server -d $database -E -b -Q $query 2>$null
  if ($LASTEXITCODE -ne 0) { throw 'Backup checksum verification failed.' }
}
function ReadApi { return Invoke-RestMethod -Uri "$api/api/settings" -Headers $headers -TimeoutSec 20 }
function ApiValue($response,[string]$id) { $item = $response.items | Where-Object { $_.parameterId -ceq $id } | Select-Object -First 1; return $item.value }
function Save($change) { $body = ConvertTo-Json -InputObject @{ changes = @($change) } -Depth 12 -Compress; $null = Invoke-RestMethod -Method Put -Uri "$api/api/settings" -Headers $headers -ContentType 'application/json' -Body $body -TimeoutSec 30 }
function Ext([string]$key) {
  $allowed = @('ParameterProductDetailNotLogin2','ParameterMenu2','WhatsAppOrderNotification','FavoriteIconPosition','FavoriteIconVerticalPosition')
  if ($key -cnotin $allowed) { throw 'Extension key is outside fixed verification allowlist.' }
  $query = "SET NOCOUNT ON; SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.DealerSystemSettingExtension WHERE MemberCode=N'store5' AND SettingKey=N'$key') THEN N'PRESENT:'+(SELECT COALESCE(SettingValue,N'__NULL__') FROM dbo.DealerSystemSettingExtension WHERE MemberCode=N'store5' AND SettingKey=N'$key') ELSE N'__MISSING__' END;"
  return Db $query
}
function DeleteExtension([string]$key) {
  $allowed = @('ParameterProductDetailNotLogin2','ParameterMenu2','WhatsAppOrderNotification','FavoriteIconPosition','FavoriteIconVerticalPosition')
  if ($key -cnotin $allowed) { throw 'Extension restore key is outside fixed allowlist.' }
  $query = "SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION; DELETE dbo.DealerSystemSettingExtension WHERE MemberCode=N'store5' AND SettingKey=N'$key'; IF @@ROWCOUNT<>1 BEGIN ROLLBACK TRANSACTION; THROW 53310,'restore',1; END; COMMIT TRANSACTION; SELECT 'OK';"
  $result = Db $query
  if ($result -ne 'OK') { throw 'Extension row restoration failed.' }
}
function RunScalar([string]$id,[string]$key,[string]$type,[string]$label) {
  $baseline = ApiValue (ReadApi) $id
  $sqlBaseline = Ext $key
  if ($null -ne $baseline -or $sqlBaseline -cne '__MISSING__') { throw 'Expected unconfigured scalar baseline differs.' }
  Backup $label
  if ($type -eq 'boolean') { $value = $false; $stored = 'false' }
  elseif ($type -eq 'horizontal') { $value = 'rbFavoriteIconLeft'; $stored = 'Left' }
  else { $value = 'rbFavoriteIconTop'; $stored = 'Top' }
  try {
    Save @{ parameterId=$id; expectedValue=$null; value=$value }
    if ((ApiValue (ReadApi) $id) -cne $value -or (Ext $key) -cne "PRESENT:$stored") { throw 'Scalar API/SQL readback failed.' }
    if ($type -eq 'boolean') {
      Save @{ parameterId=$id; expectedValue=$false; value=$true }
      if ((ApiValue (ReadApi) $id) -ne $true -or (Ext $key) -cne 'PRESENT:true') { throw 'Explicit false/true check failed.' }
    }
  } finally {
    if ((Ext $key) -ne '__MISSING__') { DeleteExtension $key }
    if ($null -ne (ApiValue (ReadApi) $id) -or (Ext $key) -cne $sqlBaseline) { throw 'Scalar exact restore failed.' }
    AssertTarget
  }
  Write-Output "PASS $id - API/SQL readback and unconfigured restoration verified"
}
function RunMask([string]$id,[string]$key,[string]$bit,[string]$label) {
  $baseline = ApiValue (ReadApi) $id
  $sqlBaseline = Ext $key
  if (($null -eq $baseline) -ne ($sqlBaseline -eq '__MISSING__')) { throw 'API/SQL baseline presence mismatch.' }
  if ($null -ne $baseline -and $sqlBaseline -cne "PRESENT:$baseline") { throw 'API/SQL baseline mismatch.' }
  Backup $label
  $b = [Numerics.BigInteger]::Parse($bit)
  $old = [Numerics.BigInteger]::Zero
  if ($null -ne $baseline) { $old = [Numerics.BigInteger]::Parse([string]$baseline) }
  $wasOn = (($old / $b) % 2) -eq 1
  $next = $old
  if ($wasOn) { $next = $old - $b } else { $next = $old + $b }
  $expected = $null
  if ($null -ne $baseline) { $expected = [string]$baseline }
  try {
    Save @{ parameterId=$id; expectedValue=$expected; value=$null; bitOperations=@(@{ optionValue=$bit; enabled=(-not $wasOn) }) }
    $newValue = $next.ToString()
    if ([string](ApiValue (ReadApi) $id) -cne $newValue -or (Ext $key) -cne "PRESENT:$newValue") { throw 'Mask API/SQL readback failed.' }
    $oldRest = $old
    if ($wasOn) { $oldRest = $old - $b }
    $newRest = $next
    if (-not $wasOn) { $newRest = $next - $b }
    if ($oldRest -ne $newRest) { throw 'Unrelated mask bits changed.' }
  } finally {
    if ($null -eq $baseline) {
      if ((Ext $key) -ne '__MISSING__') { DeleteExtension $key }
    } else {
      $current = [Numerics.BigInteger]::Parse([string](ApiValue (ReadApi) $id))
      $currentOn = (($current / $b) % 2) -eq 1
      if ($currentOn -ne $wasOn) { Save @{ parameterId=$id; expectedValue=$current.ToString(); value=$null; bitOperations=@(@{ optionValue=$bit; enabled=$wasOn }) } }
    }
    $restored = ApiValue (ReadApi) $id
    if (($null -eq $restored) -ne ($null -eq $baseline)) { throw 'Mask restore presence check failed.' }
    if ($null -ne $baseline -and [string]$restored -cne [string]$baseline) { throw 'Mask exact restore failed.' }
    if ((Ext $key) -cne $sqlBaseline) { throw 'Mask SQL restore failed.' }
    AssertTarget
  }
  Write-Output "PASS $id - API/SQL readback, bit preservation and restoration verified"
}
$cap = Invoke-RestMethod -Uri "$api/api/settings/capabilities" -Headers $headers -TimeoutSec 10
if ($cap.environment -ne 'IsolatedLocalDemo' -or $cap.target -ne 'Store5_Settings_Test / store5' -or -not $cap.canSaveSettings) { throw 'The API is not in the approved isolated write mode.' }
if ((ReadApi).items.Count -ne 98) { throw 'Expected 98 API settings.' }
RunMask 'ctl00_PageContent_ddlParameterProductDetailNotLogin2' 'ParameterProductDetailNotLogin2' '1' 'ProductDetailNotLogin2'
RunMask 'ctl00_PageContent_ddlParameterMenu2' 'ParameterMenu2' '1' 'ParameterMenu2'
RunScalar 'ctl00_PageContent_chkWhatsAppOrderNotification' 'WhatsAppOrderNotification' 'boolean' 'WhatsAppOrderNotification'
RunScalar 'ctl00_PageContent_rbFavoriteIconLeft' 'FavoriteIconPosition' 'horizontal' 'FavoriteIconPosition'
RunScalar 'ctl00_PageContent_rbFavoriteIconTop' 'FavoriteIconVerticalPosition' 'vertical' 'FavoriteIconVerticalPosition'
