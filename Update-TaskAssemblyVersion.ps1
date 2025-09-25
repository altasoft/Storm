Param (
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,
    [Parameter(Mandatory=$true)]
    [string]$FilePath
)

# Use the provided file path
$xmlPath = $FilePath

# Load the XML file as a raw string to preserve formatting
[xml]$xmlContent = Get-Content $xmlPath -Raw

# Create a namespace manager for the default namespace
$ns = New-Object System.Xml.XmlNamespaceManager($xmlContent.NameTable)
$ns.AddNamespace("ms", "http://schemas.microsoft.com/developer/msbuild/2003")

# Select the TaskAssembly node using the namespace
$taskAssemblyNode = $xmlContent.SelectSingleNode("//ms:TaskAssembly", $ns)

if ($taskAssemblyNode -ne $null) {
    $oldText = $taskAssemblyNode.InnerText
    
    # The version number is embedded in the string after "altasoft.storm.generator.mssql\" and before "\tasks"
    $pattern = '(altasoft\.storm\.generator\.mssql\\)([\d\.]+)(\\tasks)'
    
    # Replace the current version with the new version using a script block for the replacement
    $newText = [regex]::Replace($oldText, $pattern, { param($match)
        return "$($match.Groups[1].Value)$NewVersion$($match.Groups[3].Value)"
    })
    
    # Update the TaskAssembly node text with the new version
    $taskAssemblyNode.InnerText = $newText
    
    # Save the changes back to the file
    $xmlContent.Save($xmlPath)

    Write-Host "TaskAssembly version updated to $NewVersion in $xmlPath"
} else {
    Write-Host "TaskAssembly element not found in $xmlPath"
}
