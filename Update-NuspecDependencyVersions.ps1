param(
    [string]$PackageVersion
)

# Path to the nuspec and Directory.Packages.props files
$nuspecPath = "src\AltaSoft.Storm.MsSql\AltaSoft.Storm.MsSql.nuspec"
$propsPath = "Directory.Packages.props"

# Load the props file and build a hashtable of package versions
[xml]$propsXml = Get-Content $propsPath
$packageVersions = @{}
$propsXml.Project.ItemGroup.PackageVersion | ForEach-Object {
    $id = $_.Include
    $ver = $_.Version
    if ($id -and $ver) { $packageVersions[$id] = $ver }
}

# Load the nuspec file
[xml]$nuspecXml = Get-Content $nuspecPath

# Update dependency versions
$nuspecXml.package.metadata.dependencies.group.dependency | ForEach-Object {
    $id = $_.id
    if ($packageVersions.ContainsKey($id)) {
        $_.version = $packageVersions[$id]
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
