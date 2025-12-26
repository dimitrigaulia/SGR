# Script de deploy do SGR na VPS Hostinger (PowerShell)
# Uso: .\deploy.ps1

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Deploy do SGR na VPS Hostinger" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar se Docker está instalado
try {
    docker --version | Out-Null
    Write-Host "✓ Docker encontrado" -ForegroundColor Green
} catch {
    Write-Host "Erro: Docker não está instalado." -ForegroundColor Red
    exit 1
}

# Verificar se Docker Compose está instalado
try {
    docker compose version | Out-Null
    $composeCommand = "docker compose"
    Write-Host "✓ Docker Compose encontrado" -ForegroundColor Green
} catch {
    try {
        docker-compose --version | Out-Null
        $composeCommand = "docker-compose"
        Write-Host "✓ Docker Compose encontrado" -ForegroundColor Green
    } catch {
        Write-Host "Erro: Docker Compose não está instalado." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# Parar containers existentes (se houver)
Write-Host "Parando containers existentes..."
& $composeCommand.Split(' ') down 2>$null
Write-Host "✓ Containers parados" -ForegroundColor Green
Write-Host ""

# Build das imagens
Write-Host "Construindo imagens Docker..."
& $composeCommand.Split(' ') build --no-cache
Write-Host "✓ Imagens construídas" -ForegroundColor Green
Write-Host ""

# Iniciar serviços
Write-Host "Iniciando serviços..."
& $composeCommand.Split(' ') up -d
Write-Host "✓ Serviços iniciados" -ForegroundColor Green
Write-Host ""

# Aguardar serviços ficarem prontos
Write-Host "Aguardando serviços ficarem prontos..."
Start-Sleep -Seconds 10

# Verificar status dos containers
Write-Host ""
Write-Host "Status dos containers:"
& $composeCommand.Split(' ') ps

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Deploy concluído com sucesso!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Acesse a aplicação em: http://31.97.247.109" -ForegroundColor Yellow
Write-Host ""
Write-Host "Para ver os logs:" -ForegroundColor Cyan
Write-Host "  $composeCommand logs -f"
Write-Host ""
Write-Host "Para parar os serviços:" -ForegroundColor Cyan
Write-Host "  $composeCommand down"
Write-Host ""

