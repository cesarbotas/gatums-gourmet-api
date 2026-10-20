#!/bin/bash
set -e

# ============================================================
#  Gatums Gourmet — Deploy na VPS Locaweb
#  Executado no servidor (via SSH pelo GitHub Action ou manual)
# ============================================================

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

DEPLOY_PATH="${DEPLOY_PATH:-$(pwd)}"

echo ""
echo "=========================================="
echo "  Gatums Gourmet — Deploy Locaweb"
echo "=========================================="
echo ""

# 1. Verificar .env
echo -e "${YELLOW}[1/4] Verificando configuração...${NC}"
if [ ! -f "${DEPLOY_PATH}/.env" ]; then
    echo -e "${RED}Arquivo .env não encontrado em ${DEPLOY_PATH}!${NC}"
    echo "Crie o arquivo .env baseado no .env.example:"
    echo "  cp ${DEPLOY_PATH}/.env.example ${DEPLOY_PATH}/.env"
    echo "  nano ${DEPLOY_PATH}/.env"
    exit 1
fi
echo -e "${GREEN}Arquivo .env encontrado ✅${NC}"

# 2. Parar containers antigos
echo -e "${YELLOW}[2/4] Parando containers antigos...${NC}"
docker compose -f "${DEPLOY_PATH}/docker-compose.production.yml" --env-file "${DEPLOY_PATH}/.env" down 2>/dev/null || true
echo -e "${GREEN}Containers antigos parados ✅${NC}"

# 3. Build e deploy
echo -e "${YELLOW}[3/4] Subindo containers...${NC}"
docker compose -f "${DEPLOY_PATH}/docker-compose.production.yml" --env-file "${DEPLOY_PATH}/.env" up -d --build
echo -e "${GREEN}Containers iniciados ✅${NC}"

# 4. Aguardar e verificar
echo -e "${YELLOW}[4/4] Verificando serviços...${NC}"
sleep 15

if docker ps --format '{{.Names}}' | grep -q gatums-api; then
    echo -e "${GREEN}API rodando ✅${NC}"
else
    echo -e "${RED}API com problema ❌${NC}"
    docker logs gatums-api --tail 30
    exit 1
fi

if docker ps --format '{{.Names}}' | grep -q gatums-db; then
    echo -e "${GREEN}PostgreSQL rodando ✅${NC}"
else
    echo -e "${RED}PostgreSQL com problema ❌${NC}"
    exit 1
fi

# Health check (endpoint público)
API_STATUS=$(curl -s -o /dev/null -w '%{http_code}' http://localhost/api/v1/produtos/categorias 2>/dev/null || echo "000")
if [ "$API_STATUS" = "200" ]; then
    echo -e "${GREEN}Health check OK (HTTP 200) ✅${NC}"
else
    echo -e "${YELLOW}Health check retornou ${API_STATUS} (API pode estar inicializando)${NC}"
    echo -e "${YELLOW}Últimos logs:${NC}"
    docker logs gatums-api --tail 10
fi

SERVER_IP=$(hostname -I | awk '{print $1}')

echo ""
echo "=========================================="
echo -e "${GREEN}  Deploy concluído! 🚀${NC}"
echo "=========================================="
echo ""
echo "  🌐 API:        http://${SERVER_IP}/api/v1"
echo "  🏥 Health:     http://${SERVER_IP}/api/v1/produtos/categorias"
echo "  📦 PostgreSQL: localhost:5432 (interno)"
echo ""
echo "  📋 Comandos úteis:"
echo "     Logs API:    docker logs -f gatums-api"
echo "     Logs DB:     docker logs -f gatums-db"
echo "     Parar:       docker compose -f docker-compose.production.yml down"
echo "     Reiniciar:   docker compose -f docker-compose.production.yml restart"
echo ""
