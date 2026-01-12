param(
    [string]$PackageVersion
)

# Path to the nuspec and Directory.Packages.props files
$nuspecPath = "src\AltaSoft.Storm.MsSql\AltaSoft.Storm.MsSql.nuspec"
$propsPath = "Directory.Packages.props"

# Load the props file and build mappings of package versions.
# We support both unconditional PackageVersion entries and entries conditioned on TargetFramework.
[xml]$propsXml = Get-Content $propsPath
$packageVersionsDefault = @{}
$packageVersionsByTf = @{}

# Helper: add a version for package id and targetFramework
function Add-PackageVersionForTf([string]$id, [string]$tf, [string]$ver) {
    if (-not $packageVersionsByTf.ContainsKey($id)) { $packageVersionsByTf[$id] = @{} }
    $packageVersionsByTf[$id][$tf] = $ver
}

# Iterate PackageVersion nodes and detect Condition either on node or on parent ItemGroup.
$propsXml.Project.ItemGroup.PackageVersion | ForEach-Object {
    $id = $_.Include
    $ver = $_.Version
    $cond = $_.Condition

    # If element doesn't have Condition, try parent ItemGroup's Condition
    if (-not $cond) {
        try { $cond = $_.ParentNode.Condition } catch { $cond = $null }
    }

    if ($id -and $ver) {
        if ($cond) {
            # extract all quoted values from condition (handles "== 'net8.0' or 'net9.0'" and semicolon lists)
            $matches = [regex]::Matches($cond, "'([^']+)'")
            if ($matches.Count -gt 0) {
                foreach ($m in $matches) {
                    $val = $m.Groups[1].Value
                    # split semicolon-separated TF lists (common in TF properties)
                    $tfs = $val -split ';'
                    foreach ($tf in $tfs) {
                        $tfTrim = $tf.Trim()
                        if ($tfTrim) { Add-PackageVersionForTf $id $tfTrim $ver }
                    }
                }
                return
            }

            # If no quoted tokens found, fall back to default mapping
            $packageVersionsDefault[$id] = $ver
        }
        else {
            $packageVersionsDefault[$id] = $ver
        }
    }
}

# Load the nuspec file
[xml]$nuspecXml = Get-Content $nuspecPath

# Helper to update a dependency node with respect to targetFramework
function Update-DependencyVersion([System.Xml.XmlElement]$depNode, [string]$targetFramework) {
    # Use attribute accessors for XmlElement to avoid type coercion issues in PowerShell
    $id = $depNode.GetAttribute('id')
    if (-not $id) { return }

    # Prefer a target-framework specific version when available
    if ($targetFramework -and $packageVersionsByTf.ContainsKey($id) -and $packageVersionsByTf[$id].ContainsKey($targetFramework)) {
        $depNode.SetAttribute('version', $packageVersionsByTf[$id][$targetFramework])
    }
    elseif ($packageVersionsDefault.ContainsKey($id)) {
        $depNode.SetAttribute('version', $packageVersionsDefault[$id])
    }
    # otherwise leave existing version in nuspec as-is
}

$depsRoot = $nuspecXml.package.metadata.dependencies
if ($null -ne $depsRoot) {
    # Handle grouped dependencies (with optional targetFramework attribute)
    if ($depsRoot.group) {
        foreach ($group in $depsRoot.group) {
            # group may have an attribute targetFramework
            $tf = ''
            try {
                $tf = $group.GetAttribute('targetFramework')
            }
            catch {
                # older XML PS type may expose it differently; try property access
                $tf = $group.targetFramework
            }

            if ($group.dependency) {
                foreach ($dep in $group.dependency) {
                    Update-DependencyVersion $dep $tf
                }
            }
        }
    }

    # Handle dependencies directly under <dependencies> (no group)
    if ($depsRoot.dependency) {
        foreach ($dep in $depsRoot.dependency) {
            Update-DependencyVersion $dep ''
        }
    }
}

# Update the package version if parameter is provided
if ($PackageVersion) {
    $nuspecXml.package.metadata.version = $PackageVersion
}

# Save the updated nuspec file as UTF-8 with indentation
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$xmlWriterSettings = New-Object System.Xml.XmlWriterSettings
$xmlWriterSettings.Indent = $true
$xmlWriterSettings.Encoding = $utf8NoBom

$writer = [System.Xml.XmlWriter]::Create($nuspecPath, $xmlWriterSettings)
$nuspecXml.Save($writer)
$writer.Close()

Write-Host "Updated dependency versions in $nuspecPath"
