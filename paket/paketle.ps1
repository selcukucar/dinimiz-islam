param(
    [string]$Dotnet = 'dotnet',
    [string]$Iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    [string]$Cikti = '',
    [switch]$TestleriAtla
)
$ErrorActionPreference = 'Stop'
$kok = Split-Path $PSScriptRoot -Parent
if (!$Cikti) { $Cikti = Join-Path $kok 'artifacts' }
$Cikti = [IO.Path]::GetFullPath($Cikti)
$surum = ([xml](Get-Content (Join-Path $kok 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
$yayin = Join-Path $Cikti ('yayin-' + $surum)
if (!(Test-Path -LiteralPath $Iscc -PathType Leaf)) { throw "Inno Setup derleyicisi bulunamadi: $Iscc" }
New-Item -ItemType Directory -Force $Cikti, $yayin | Out-Null
Push-Location $kok
try {
    if (!$TestleriAtla) {
        & $Dotnet test tests/DinimizIslam.Testler -c Release -m:1 --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Testler basarisiz.' }
    }
    & $Dotnet publish src/DinimizIslam.Arayuz -c Release -r win-x64 --self-contained true -m:1 -p:DebugType=none -p:DebugSymbols=false -o $yayin
    if ($LASTEXITCODE -ne 0) { throw 'Yayinlama basarisiz.' }
    # Runtime LICENSE.txt dosyasinin uzerine yazmayin.
    Copy-Item LICENSE (Join-Path $yayin 'UYGULAMA-LISANSI.txt') -Force
    Copy-Item README.md, NOTICE.md, CHANGELOG.md $yayin -Force
    Copy-Item docs (Join-Path $yayin 'docs') -Recurse -Force
    Copy-Item licenses (Join-Path $yayin 'licenses') -Recurse -Force
    & $Iscc "/DSurum=$surum" "/DYayin=$yayin" "/DCikti=$Cikti" paket/DinimizIslam.iss
    if ($LASTEXITCODE -ne 0) { throw 'Kurulum paketi derlenemedi.' }
    $zip = Join-Path $Cikti "DinimizIslam-WPF-$surum-Tasinabilir-x64.zip"
    Compress-Archive -Path (Join-Path $yayin '*') -DestinationPath $zip -Force
    $kaynak = Join-Path $Cikti "DinimizIslam-WPF-$surum-Kaynak.zip"
    # Yalniz acikca secilmis kaynaklar; bin/obj, veri, SDK ve yayin dosyalari haric.
    $secili = @('src','tests','paket','docs','licenses','.github','README.md','NOTICE.md','LICENSE','CHANGELOG.md','.gitignore','Directory.Build.props','global.json','NuGet.Config','DinimizIslam.slnx')
    Add-Type -AssemblyName System.IO.Compression
    $stream = [IO.File]::Open($kaynak, [IO.FileMode]::Create)
    $arsiv = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($girdi in $secili) {
            $yol = Join-Path $kok $girdi
            $dosyalar = if (Test-Path -LiteralPath $yol -PathType Container) { Get-ChildItem -LiteralPath $yol -File -Recurse -Force } else { Get-Item -LiteralPath $yol -Force }
            foreach ($dosya in $dosyalar) {
                $ad = $dosya.FullName.Substring($kok.Length + 1).Replace('\','/')
                if ($ad -match '/(bin|obj)/') { continue }
                $entry = $arsiv.CreateEntry($ad, [IO.Compression.CompressionLevel]::Optimal)
                $hedef = $entry.Open(); $okuma = $dosya.OpenRead()
                try { $okuma.CopyTo($hedef) } finally { $okuma.Dispose(); $hedef.Dispose() }
            }
        }
    } finally { $arsiv.Dispose(); $stream.Dispose() }
    $dosyalar = @($zip, $kaynak, (Join-Path $Cikti "DinimizIslam-WPF-$surum-Kurulum-x64.exe"))
    $dosyalar | ForEach-Object { '{0}  {1}' -f (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant(), [IO.Path]::GetFileName($_) } | Set-Content (Join-Path $Cikti 'SHA256SUMS.txt') -Encoding ascii
    $dosyalar | Get-Item | Select-Object Name, Length
} finally { Pop-Location }
