# required parameters :
# 	$databaseName

properties {
	$visualstudioversion = null-coalesce $visualstudioversion "12.0"
	$projectName = "DTMF"
    $version = null-coalesce $version ("1.0." + (get-date -format "yyyy")) #changed to three numbers since that's how version comes from teamcity
    $fullversion = $version + "." + (get-date -format "MMdd")  

	$projectConfig = "Release"
	$base_dir = resolve-path .
	$source_dir = "$base_dir\source"
	$webapp_dir = "$source_dir\$projectName.Website" 
	$zipPath = "$base_dir\lib\7zip\7za.exe"
	$build_dir = null-coalesce $build_dir "$base_dir\build"
	$package_dir = "$build_dir\Latest"	
	$package_file = "$build_dir\" + $projectName + "-" + $version +"_package.zip"
}

task default -depends Init, Compile
task ci -depends Init, Compile, Package


task Init {
    #delete the build directories, but leave any artifacts at the root then put it back
    delete_directory "$build_dir"
    create_directory "$build_dir"
}

task Compile -depends Init {
    write-host "Restore solution-level nuget packages"
    exec { & $source_dir\.nuget\nuget.exe restore $source_dir\$projectName.sln }
	
    Update-AssemblyInfoFiles $fullversion
    msbuild /t:clean /v:q /nologo /p:Configuration=$projectConfig $source_dir\$projectName.sln /p:VisualStudioVersion=$visualstudioversion 

    msbuild /t:build /v:q /nologo /p:Configuration=$projectConfig $source_dir\$projectName.sln /p:VisualStudioVersion=$visualstudioversion
    #moves all .nupkg in $source_dir and not in packages folder to $build_dir
	Get-ChildItem -Path $source_dir\*.nupkg -Recurse | Where-Object { $_.FullName -notmatch '\\packages($|\\)' } | Move-Item -Destination $build_dir -Force

    copy_website_files "$webapp_dir" "$package_dir\web"

    Update-AssemblyInfoFiles ($version.Replace($version.Split('.')[-1], "0") + ".0") #cleans the version's year to something like 3.0.0.0
}



task Package -depends Compile {

    write-host "Clean package directory"
    delete_directory $package_dir
   
    write-host "Copy web app"
    copy_website_files "$webapp_dir" "$package_dir\web" 

    write-host "Zip it up"
	zip_directory $package_dir $package_file 
}
 
function global:zip_directory($directory,$file) {
    write-host "Zipping folder: " $test_assembly
    delete_file $file
    cd $directory
    & "$zipPath" a -mx=9 -r $file
    cd $base_dir
}

function global:copy_website_files($source,$destination){
    $exclude = @('*.user','*.dtd','*.tt','*.cs','*.csproj','*.vb','*.vbproj','*.orig', '*.log') 
    copy_files $source $destination $exclude
	delete_directory "$destination\obj"
     while (Get-ChildItem $destination -recurse | where {!@(Get-ChildItem -force $_.fullname)} | Test-Path) {
        Get-ChildItem $destination -recurse | where {!@(Get-ChildItem -force $_.fullname)} | Remove-Item
    }
}

function global:copy_files($source,$destination,$exclude=@()){    
    create_directory $destination
    Get-ChildItem $source -Recurse -Exclude $exclude -ErrorAction SilentlyContinue | Copy-Item -ErrorAction SilentlyContinue -Destination {Join-Path $destination $_.FullName.Substring($source.length)} 
}

function global:Copy_and_flatten ($source,$filter,$dest) {
  ls "$source" -filter "$filter"  -r | Where-Object{!$_.FullName.Contains("$testCopyIgnorePath") -and !$_.FullName.Contains("packages") }| cp -dest "$dest" -force
}

function global:copy_all_assemblies_for_test($destination){
  create_directory "$destination"
  Copy_and_flatten "$source_dir" *.exe "$destination"
  Copy_and_flatten "$source_dir" *.dll "$destination"
  Copy_and_flatten "$source_dir" *.config "$destination"
  Copy_and_flatten "$source_dir" *.xml "$destination"
  Copy_and_flatten "$source_dir" *.pdb "$destination"
  #Copy_and_flatten $source_dir *.sql $destination
}

function global:delete_file($file) {
    if($file) { remove-item $file -force -ErrorAction SilentlyContinue | out-null } 
}

function global:delete_directory($directory_name)
{
  rd $directory_name -recurse -force  -ErrorAction SilentlyContinue | out-null
}

function global:delete_files_in_dir($dir)
{
	get-childitem $dir -recurse | foreach ($_) {remove-item $_.fullname}
}

function global:create_directory($directory_name)
{
  mkdir $directory_name  -ErrorAction SilentlyContinue  | out-null
}

function Update-AssemblyInfoFiles ([string] $version, [System.Array] $excludes = $null) {
 
#-------------------------------------------------------------------------------
# Update version numbers of AssemblyInfo.cs
# adapted from: http://www.luisrocha.net/2009/11/setting-assembly-version-with-windows.html
#-------------------------------------------------------------------------------
 
	if ($version -notmatch "[0-9]+(\.([0-9]+|\*)){1,3}") {
		Write-Error "Version number incorrect format: $version"
	}
	
	$versionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
	$versionAssembly = 'AssemblyVersion("' + $version + '")';
	$versionFilePattern = 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
	$versionAssemblyFile = 'AssemblyFileVersion("' + $version + '")';
 
	Get-ChildItem -r -filter AssemblyInfo.* | % {
		$filename = $_.fullname
		
		$update_assembly_and_file = $true
		
		# set an exclude flag where only AssemblyFileVersion is set
		if ($excludes -ne $null)
			{ $excludes | % { if ($filename -match $_) { $update_assembly_and_file = $false	} } }
		
		# see http://stackoverflow.com/questions/3057673/powershell-locking-file
		# I am getting really funky locking issues. 
		# The code block below should be:
		#     (get-content $filename) | % {$_ -replace $versionPattern, $version } | set-content $filename
 
		$tmp = ($file + ".tmp")
		if (test-path ($tmp)) { remove-item $tmp }
 
		if ($update_assembly_and_file) {
			(get-content $filename) | % {$_ -replace $versionFilePattern, $versionAssemblyFile } | % {$_ -replace $versionPattern, $versionAssembly }  > $tmp
			write-host Updating file AssemblyInfo and AssemblyFileInfo: $filename --> $versionAssembly / $versionAssemblyFile
		} else {
			(get-content $filename) | % {$_ -replace $versionFilePattern, $versionAssemblyFile } > $tmp
			write-host Updating file AssemblyInfo only: $filename --> $versionAssemblyFile
		}
 
		if (test-path ($filename)) { remove-item $filename }
        #diff tools aren't too happy with Unicode files - change it to ansi
        Set-Content $tmp -Encoding ASCII -Value (Get-Content $tmp)
		move-item $tmp $filename -force		
 
	}
}

function null-coalesce($a, $b) { if ($a -ne $null) { $a } else { $b } }