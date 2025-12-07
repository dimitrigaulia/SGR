# Script para converter arquivos para UTF-8 sem BOM
# Preserva o conteúdo original durante a conversão

$rootPath = Get-Location | Select-Object -ExpandProperty Path
$reportPath = Join-Path $rootPath "encoding-report.txt"

Write-Host "Convertendo arquivos para UTF-8 sem BOM..." -ForegroundColor Cyan
Write-Host "Diretório raiz: $rootPath" -ForegroundColor Gray
Write-Host ""

# Função para converter arquivo para UTF-8 sem BOM
function Convert-ToUtf8NoBom {
    param([string]$FilePath)
    
    try {
        # Ler o conteúdo do arquivo tentando detectar o encoding
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
        
        # Se tem BOM UTF-8, remover os primeiros 3 bytes
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            $contentBytes = $bytes[3..($bytes.Length - 1)]
            $content = [System.Text.Encoding]::UTF8.GetString($contentBytes)
        }
        # Se tem BOM UTF-16 LE, converter
        elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
            $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::Unicode)
        }
        # Se tem BOM UTF-16 BE, converter
        elseif ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF) {
            $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::BigEndianUnicode)
        }
        # Tentar Windows-1252
        else {
            try {
                $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::GetEncoding(1252))
            }
            catch {
                # Se falhar, tentar UTF-8
                try {
                    $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)
                }
                catch {
                    # Último recurso: usar encoding padrão do sistema
                    $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::Default)
                }
            }
        }
        
        # Salvar como UTF-8 sem BOM
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($FilePath, $content, $utf8NoBom)
        
        return $true
    }
    catch {
        Write-Host "Erro ao converter $FilePath : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Ler o relatório para obter lista de arquivos
$incorrectFiles = @()
if (Test-Path $reportPath) {
    $reportContent = Get-Content $reportPath -Raw
    $lines = Get-Content $reportPath
    
    $currentFile = $null
    foreach ($line in $lines) {
        if ($line -match "^Arquivo: (.+)$") {
            $currentFile = $matches[1]
        }
        elseif ($line -match "^Encoding atual: (.+)$" -and $currentFile) {
            $fullPath = Join-Path $rootPath $currentFile
            if (Test-Path $fullPath) {
                $incorrectFiles += $fullPath
            }
            $currentFile = $null
        }
    }
}

Write-Host "Total de arquivos a converter: $($incorrectFiles.Count)" -ForegroundColor Cyan
Write-Host ""

$successCount = 0
$errorCount = 0

foreach ($filePath in $incorrectFiles) {
    $relativePath = $filePath.Replace($rootPath, '').TrimStart('\')
    Write-Host "Convertendo: $relativePath" -ForegroundColor Yellow
    
    if (Convert-ToUtf8NoBom -FilePath $filePath) {
        $successCount++
        Write-Host "  [OK] Convertido com sucesso" -ForegroundColor Green
    } else {
        $errorCount++
        Write-Host "  [ERRO] Falha na conversão" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RESUMO DA CONVERSÃO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total de arquivos processados: $($incorrectFiles.Count)" -ForegroundColor White
Write-Host "Conversões bem-sucedidas: $successCount" -ForegroundColor Green
Write-Host "Erros: $errorCount" -ForegroundColor Red
Write-Host ""

if ($successCount -gt 0) {
    Write-Host "Reexecutando verificação para confirmar..." -ForegroundColor Cyan
    & (Join-Path $rootPath "scripts\check-encoding.ps1")
}

