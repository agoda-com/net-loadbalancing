#$ErrorActionPreference = "Stop"

$version = $env:APPVEYOR_BUILD_VERSION


if(!$version) 
{
	Write-Output "Missing version: $version"
	Exit 1
}


$branch = $env:APPVEYOR_REPO_BRANCH


if(!$branch) 
{
	Write-Output "Missing branch: $branch"
	Exit 1
}


if($branch -ne "master") 
{
    $version = "$version-alpha"
}


$scriptDir = Split-Path -Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent) -Parent
$toolsPath = Join-Path $scriptDir "tools"

cd $toolsPath

$deployables = @{
    "DB" = "Agoda.Frameworks.DB"
    "Http" = "Agoda.Frameworks.Http"
    "Http.AutoRestExt" = "Agoda.Frameworks.Http.AutoRestExt"
    "LoadBalancing" = "Agoda.Frameworks.LoadBalancing"
}

function Configure_Meta($meta, $include, $v) {
    if(![string]::IsNullOrWhiteSpace($include)){        
        $dependency = $meta.OwnerDocument.CreateElement("dependency")
        $id = $meta.OwnerDocument.CreateAttribute("id")
        $id.Value = $include
        $vrs = $meta.OwnerDocument.CreateAttribute("version")
        $vrs.Value = $v
        [void]$dependency.Attributes.Append($id)
        [void]$dependency.Attributes.Append($vrs)
        [void]$dependencies.AppendChild($dependency)      
    }   
}

function Append-FileNode($files, $file, $lib) {
    $node = $files.OwnerDocument.CreateElement("file")  
    $src = $files.OwnerDocument.CreateAttribute("src")
    $src.Value = $file.FullName
    $target = $files.OwnerDocument.CreateAttribute("target")
    $target.Value = $lib
    [void]$node.Attributes.Append($src)
    [void]$node.Attributes.Append($target)
    [void]$files.AppendChild($node)   
}

$deployables.GetEnumerator() | % {
    $name = $_.Value
    $projectPath = Join-Path $scriptDir $name    
    $csProjPath = Join-Path $projectPath "$name.csproj"
    [xml]$proj = gc $csProjPath -Raw    
    
    cd $projectPath
    $nuspecPath = Join-Path $projectPath "$name.nuspec"
    if(Test-Path $nuspecPath) {
        Remove-Item $nuspecPath
    }

    nuget spec    
    [xml]$nuspec = gc $nuspecPath -Raw
    $meta = $nuspec.package.metadata
    $meta.id = $name
    $meta.title = $name    
    $meta.version = $version
    $meta.authors = [string]$proj.Project.PropertyGroup.Authors
    $meta.owners = $meta.authors
    $meta.projectUrl = [string]$proj.Project.PropertyGroup.PackageProjectUrl
    $meta.description = [string]$proj.Project.PropertyGroup.Description
    @("licenseUrl", "iconUrl", "tags", "releaseNotes") | % {
        [void]$meta.RemoveChild($meta.SelectSingleNode($_))
    }
    $dependencies = $meta.OwnerDocument.CreateElement("dependencies")   
    $files = $meta.OwnerDocument.CreateElement("files")    

    $proj.Project.ItemGroup.PackageReference | % {      
        [string]$include = $_.Include
        [string]$v = $_.Version
        Configure_Meta $meta $include $v             
    }

    [void]$meta.AppendChild($dependencies)    

    ls (Join-Path $projectPath "bin\Debug\net462\") -Filter "$name.*" | % {
        Append-FileNode $files $_ "lib\Net462"
    }

    ls (Join-Path $projectPath "bin\Debug\netstandard2.0\") -Filter "$name.*" | % {
        Append-FileNode $files $_ "lib\netstandard2.0"
    }

    [void]$nuspec.package.AppendChild($files)

    $nuspec.Save($nuspecPath)

	dotnet pack -c Release -p:PackageVersion=$version
}