param()

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$apiBase = 'http://127.0.0.1:5173'
$sqlServer = '(localdb)\MSSQLLocalDB'
$database = 'Store5_Settings_Test'
$memberCode = 'store5'
$backupDirectory = Join-Path $env:LOCALAPPDATA 'B2BStore\backups'
$script:backupCount = 0
$script:catalog = Invoke-RestMethod -Uri "$apiBase/api/settings/catalog" -TimeoutSec 10

function Invoke-DbScalar([string]$query) {
    $lines = @(& sqlcmd -S $sqlServer -d $database -E -b -h -1 -W -Q $query 2>$null)
    if ($LASTEXITCODE -ne 0) { throw 'A scoped SQL verification query failed.' }
    return (($lines -join '').Trim())
}

function Assert-TargetPreflight {
    $query = @"
SET NOCOUNT ON;
IF DB_NAME() <> N'Store5_Settings_Test' THROW 52000, 'Unexpected database.', 1;
IF (SELECT COUNT_BIG(*) FROM dbo.DealerSettings WHERE MemberCode=N'store5') <> 1 THROW 52001, 'Target row count mismatch.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.DealerSettings WHERE MemberCode=N'store5' AND ProductCompany IS NULL) THROW 52002, 'ProductCompany baseline mismatch.', 1;
SELECT 'OK';
"@
    if ((Invoke-DbScalar $query) -ne 'OK') { throw 'The approved SQL preflight failed.' }
}

function New-VerifiedBackup([string]$label) {
    Assert-TargetPreflight
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-ffff'
    $path = [IO.Path]::GetFullPath((Join-Path $backupDirectory "Store5_Settings_Test_${label}_$stamp.bak"))
    $repositoryRoot = (Resolve-Path (Get-Location).Path).Path.TrimEnd('\') + '\'
    if ($path.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'SQL backup path must remain outside the repository.' }
    $escaped = $path.Replace("'", "''")
    $query = "SET NOCOUNT ON; BACKUP DATABASE [Store5_Settings_Test] TO DISK=N'$escaped' WITH COPY_ONLY,CHECKSUM,INIT; RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM; SELECT 'OK';"
    $null = Invoke-DbScalar $query
    $script:backupCount++
}

function Get-ApiSettings {
    return Invoke-RestMethod -Uri "$apiBase/api/settings" -TimeoutSec 20
}

function Get-ApiValue($settings, [string]$parameterId) {
    return ($settings.items | Where-Object parameterId -eq $parameterId | Select-Object -First 1).value
}

function Get-Definition([string]$parameterId) {
    foreach ($group in $script:catalog.groups) {
        $definition = $group.controls | Where-Object parameterId -eq $parameterId | Select-Object -First 1
        if ($null -ne $definition) { return $definition }
    }
    throw 'A test setting is not present in the server catalog.'
}

function Test-Bit([string]$maskText, [string]$bitText) {
    $mask = [System.Numerics.BigInteger]::Parse($maskText, [Globalization.CultureInfo]::InvariantCulture)
    $bit = [System.Numerics.BigInteger]::Parse($bitText, [Globalization.CultureInfo]::InvariantCulture)
    $quotient = [System.Numerics.BigInteger]::Divide($mask, $bit)
    return (($quotient % 2) -eq 1)
}

function Toggle-Bit([string]$maskText, [string]$bitText) {
    $mask = [System.Numerics.BigInteger]::Parse($maskText, [Globalization.CultureInfo]::InvariantCulture)
    $bit = [System.Numerics.BigInteger]::Parse($bitText, [Globalization.CultureInfo]::InvariantCulture)
    if (Test-Bit $maskText $bitText) { return ($mask - $bit).ToString([Globalization.CultureInfo]::InvariantCulture) }
    return ($mask + $bit).ToString([Globalization.CultureInfo]::InvariantCulture)
}

function Without-Bit([string]$maskText, [string]$bitText) {
    $mask = [System.Numerics.BigInteger]::Parse($maskText, [Globalization.CultureInfo]::InvariantCulture)
    $bit = [System.Numerics.BigInteger]::Parse($bitText, [Globalization.CultureInfo]::InvariantCulture)
    $width = $bit * 2
    $high = [System.Numerics.BigInteger]::Multiply([System.Numerics.BigInteger]::Divide($mask, $width), $width)
    $low = $mask % $bit
    return ($high + $low).ToString([Globalization.CultureInfo]::InvariantCulture)
}

function Invoke-ApiSave([hashtable[]]$changes) {
    $json = ConvertTo-Json -InputObject @{ changes = $changes } -Depth 12 -Compress
    $response = Invoke-RestMethod -Method Put -Uri "$apiBase/api/settings" -ContentType 'application/json' -Body $json -TimeoutSec 30
    if ($response.committed -ne $true) { throw 'The authenticated API did not confirm the isolated save.' }
}

function Get-DealerScalar([string]$column) {
    $allowed = @('ParameterPayment','ParameterProductSearch','ParameterModules')
    if ($column -notin $allowed) { throw 'A SQL column is outside the local verification allowlist.' }
    return Invoke-DbScalar "SET NOCOUNT ON; SELECT CASE WHEN [$column] IS NULL THEN N'__NULL__' ELSE CONVERT(nvarchar(64),[$column]) END FROM dbo.DealerSettings WHERE MemberCode=N'store5';"
}

function Get-ExtensionValue([string]$key) {
    $allowed = @('ParameterProductSearch.HighMask','ParameterProductDetailNotLogin2','ParameterMenu2','WhatsAppOrderNotification','FavoriteIconPosition','FavoriteIconVerticalPosition')
    if ($key -notin $allowed) { throw 'An extension key is outside the local verification allowlist.' }
    return Invoke-DbScalar "SET NOCOUNT ON; SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.DealerSystemSettingExtension WHERE MemberCode=N'store5' AND SettingKey=N'$key') THEN N'PRESENT:'+(SELECT SettingValue FROM dbo.DealerSystemSettingExtension WHERE MemberCode=N'store5' AND SettingKey=N'$key') ELSE N'__MISSING__' END;"
}

function Remove-ExtensionRowForBaseline([string]$key) {
    $allowed = @('ParameterProductSearch.HighMask','ParameterProductDetailNotLogin2','ParameterMenu2','WhatsAppOrderNotification','FavoriteIconPosition','FavoriteIconVerticalPosition')
    if ($key -notin $allowed) { throw 'An extension key is outside the local restoration allowlist.' }
    $query = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DELETE dbo.DealerSystemSettingExtension WHERE MemberCode=N'store5' AND SettingKey=N'$key';
IF @@ROWCOUNT <> 1 BEGIN ROLLBACK TRANSACTION; THROW 52010, 'Expected exactly one extension row to restore.', 1; END;
COMMIT TRANSACTION;
SELECT 'OK';
"@
    if ((Invoke-DbScalar $query) -ne 'OK') { throw 'The exact extension-row restoration failed.' }
}

function Convert-Unsigned16ToSigned([string]$value) {
    $number = [int]$value
    if ($number -lt 0 -or $number -gt 65535) { throw 'The 16-bit mask is outside its valid range.' }
    if ($number -ge 32768) { return $number - 65536 }
    return $number
}

function Test-DealerBitmask([string]$parameterId, [string]$column, [string[]]$bits) {
    $beforeResponse = Get-ApiSettings
    $baseline = [string](Get-ApiValue $beforeResponse $parameterId)
    if ([string]::IsNullOrEmpty($baseline)) { throw 'A SQL-backed bitmask baseline must be loaded before testing.' }
    $baselineSql = Get-DealerScalar $column
    $baselineHigh = if ($column -eq 'ParameterProductSearch') { Get-ExtensionValue 'ParameterProductSearch.HighMask' } else { $null }
    if ($baselineSql -eq '__NULL__') { throw 'The SQL mask unexpectedly has a NULL baseline.' }
    New-VerifiedBackup $parameterId
    $operations = @()
    $next = $baseline
    foreach ($bit in $bits) {
        $enabled = -not (Test-Bit $next $bit)
        $next = Toggle-Bit $next $bit
        $operations += @{ optionValue = $bit; enabled = $enabled }
    }
    try {
        Invoke-ApiSave @(@{ parameterId=$parameterId; expectedValue=$baseline; bitOperations=$operations })
        $saved = [string](Get-ApiValue (Get-ApiSettings) $parameterId)
        if ($saved -ne $next) { throw 'The API read-after-write did not match the expected mask.' }
        if ($column -eq 'ParameterPayment') { $expectedSql = [string](Convert-Unsigned16ToSigned $next) }
        elseif ($column -eq 'ParameterProductSearch') { $expectedSql = [string](Convert-Unsigned16ToSigned ([string]([long]$next % 65536))) }
        else { $expectedSql = $next }
        if ((Get-DealerScalar $column) -ne $expectedSql) { throw 'Direct SQL read-back did not match the expected database mask.' }
        if ($column -eq 'ParameterProductSearch') {
            $high = ([System.Numerics.BigInteger]::Parse($next) - ([System.Numerics.BigInteger]::Parse($next) % 65536)).ToString()
            if ((Get-ExtensionValue 'ParameterProductSearch.HighMask') -ne "PRESENT:$high") { throw 'The product-search high-mask SQL read-back did not match.' }
        }
    }
    finally {
        $current = [string](Get-ApiValue (Get-ApiSettings) $parameterId)
        $restoreOps = @()
        for ($index = 0; $index -lt $bits.Length; $index++) {
            $bit = $bits[$index]
            $originalEnabled = Test-Bit $baseline $bit
            $currentEnabled = Test-Bit $current $bit
            if ($originalEnabled -ne $currentEnabled) { $restoreOps += @{ optionValue=$bit; enabled=$originalEnabled } }
        }
        if ($restoreOps.Count -gt 0) { Invoke-ApiSave @(@{ parameterId=$parameterId; expectedValue=$current; bitOperations=$restoreOps }) }
        if ($column -eq 'ParameterProductSearch' -and $baselineSql -ne '__NULL__') {
            if ($baselineHigh -eq '__MISSING__') { Remove-ExtensionRowForBaseline 'ParameterProductSearch.HighMask' }
        }
        $restored = [string](Get-ApiValue (Get-ApiSettings) $parameterId)
        if ($restored -ne $baseline -or (Get-DealerScalar $column) -ne $baselineSql) { throw 'The exact SQL/API baseline restoration could not be verified.' }
        if ($column -eq 'ParameterProductSearch') {
            $finalHigh = Get-ExtensionValue 'ParameterProductSearch.HighMask'
            if ($finalHigh -ne $baselineHigh) { throw 'The product-search extension baseline could not be restored.' }
        }
        Assert-TargetPreflight
    }
    Write-Output "PASS $parameterId — authenticated API save/read-back, SQL read-back, restoration verified"
}

$capabilities = Invoke-RestMethod -Uri "$apiBase/api/settings/capabilities" -TimeoutSec 10
if ($capabilities.environment -ne 'IsolatedLocalDemo' -or $capabilities.target -ne 'Store5_Settings_Test / store5' -or $capabilities.canSaveSettings -ne $true) {
    throw 'The authenticated API is not in the exact approved isolated local test mode.'
}
if ((Get-ApiSettings).items.Count -ne 98) { throw 'The API did not load the complete settings catalog.' }
Test-DealerBitmask 'ctl00_PageContent_ddlPaymentParameter' 'ParameterPayment' @('32768')
Test-DealerBitmask 'ctl00_PageContent_ddlParameterModules' 'ParameterModules' @('1')
Test-DealerBitmask 'ctl00_PageContent_ddlProductSearchParameter' 'ParameterProductSearch' @('1','1048576')

Assert-TargetPreflight
$finalSettings = Get-ApiSettings
if ($finalSettings.items.Count -ne 98 -or (Get-ApiValue $finalSettings 'ctl00_PageContent_ddlCommonSystem') -ne $null) { throw 'The final SQL read-only baseline checks failed.' }
Write-Output "COMPLETE 3 DealerSettings field groups; $script:backupCount external backup(s) checksum-verified; target baseline restored."
