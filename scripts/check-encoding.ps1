# Script para verificar encoding de arquivos do projeto
# Verifica se todos os arquivos estão em UTF-8 sem BOM

$extensions = @('.cs', '.ts', '.html', '.json', '.md', '.scss', '.css', '.sql', '.txt', '.http')
# Usar o diretório de trabalho atual (workspace)
$rootPath = Get-Location | Select-Object -ExpandProperty Path
$reportPath = Join-Path $rootPath "encoding-report.txt"
$incorrectFiles = @()

Write-Host "Verificando encoding de arquivos..." -ForegroundColor Cyan
Write-Host "Diretório raiz: $rootPath" -ForegroundColor Gray

# Função para detectar encoding
function Get-FileEncoding {
    param([string]$FilePath)
    
    try {
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
        
        if ($bytes.Length -eq 0) {
            return "UTF-8 sem BOM"
        }
        
        # Verificar BOM
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            return "UTF-8 com BOM"
        }
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
            return "UTF-16 LE com BOM"
        }
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
            return "UTF-16 BE com BOM"
        }
        
        # Tentar ler como UTF-8 primeiro (sem BOM)
        try {
            $utf8NoBom = New-Object System.Text.UTF8Encoding $false
            $content = [System.IO.File]::ReadAllText($FilePath, $utf8NoBom)
            $reEncoded = $utf8NoBom.GetBytes($content)
            
            # Se os bytes correspondem exatamente, é UTF-8 sem BOM
            if ($bytes.Length -eq $reEncoded.Length) {
                $match = $true
                for ($i = 0; $i -lt $bytes.Length; $i++) {
                    if ($bytes[$i] -ne $reEncoded[$i]) {
                        $match = $false
                        break
                    }
                }
                if ($match) {
                    return "UTF-8 sem BOM"
                }
            }
        } catch {}
        
        # Se não funcionou como UTF-8, tentar outros encodings
        try {
            $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::Default)
            $utf8Bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
            
            # Se os bytes são diferentes, pode ser outro encoding
            if ($bytes.Length -ne $utf8Bytes.Length) {
                return "Possivelmente Windows-1252 ou outro encoding"
            }
        } catch {}
        
        # Se chegou aqui e não deu erro, provavelmente é UTF-8 sem BOM válido
        return "UTF-8 sem BOM"
    } catch {
        return "Erro ao ler arquivo: $($_.Exception.Message)"
    }
}

# Coletar todos os arquivos
$allFiles = @()
foreach ($ext in $extensions) {
    $files = Get-ChildItem -Path $rootPath -Filter "*$ext" -Recurse -File -ErrorAction SilentlyContinue | 
        Where-Object { 
            $_.FullName -notmatch '\\bin\\' -and 
            $_.FullName -notmatch '\\obj\\' -and 
            $_.FullName -notmatch '\\node_modules\\' -and
            $_.FullName -notmatch '\\.git\\'
        }
    $allFiles += $files
}

Write-Host "Total de arquivos encontrados: $($allFiles.Count)" -ForegroundColor Cyan
Write-Host ""

$correctCount = 0
$incorrectCount = 0

foreach ($file in $allFiles) {
    $encoding = Get-FileEncoding -FilePath $file.FullName
    $relativePath = $file.FullName.Replace($rootPath, '').TrimStart('\')
    
    if ($encoding -eq "UTF-8 sem BOM") {
        $correctCount++
        Write-Host "[OK] $relativePath" -ForegroundColor Green
    } else {
        $incorrectCount++
        Write-Host "[ERRO] $relativePath - Encoding: $encoding" -ForegroundColor Red
        $incorrectFiles += [PSCustomObject]@{
            Path = $relativePath
            FullPath = $file.FullName
            Encoding = $encoding
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RESUMO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total de arquivos verificados: $($allFiles.Count)" -ForegroundColor White
Write-Host "Arquivos corretos (UTF-8 sem BOM): $correctCount" -ForegroundColor Green
Write-Host "Arquivos incorretos: $incorrectCount" -ForegroundColor Red
Write-Host ""

# Gerar relatório
if ($incorrectFiles.Count -gt 0) {
    $reportContent = @"
RELATÓRIO DE VERIFICAÇÃO DE ENCODING
====================================
Data: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Total de arquivos verificados: $($allFiles.Count)
Arquivos corretos (UTF-8 sem BOM): $correctCount
Arquivos incorretos: $incorrectCount

ARQUIVOS COM ENCODING INCORRETO:
=================================

"@
    
    foreach ($file in $incorrectFiles) {
        $reportContent += "Arquivo: $($file.Path)`r`n"
        $reportContent += "Encoding atual: $($file.Encoding)`r`n"
        $reportContent += "`r`n"
    }
    
    $reportContent | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Host "Relatório salvo em: $reportPath" -ForegroundColor Yellow
} else {
    Write-Host "Todos os arquivos estão com encoding correto!" -ForegroundColor Green
    "Todos os arquivos estão com encoding correto (UTF-8 sem BOM).`r`nData: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -FilePath $reportPath -Encoding UTF8
}

# Retornar lista de arquivos incorretos para uso posterior
return $incorrectFiles

