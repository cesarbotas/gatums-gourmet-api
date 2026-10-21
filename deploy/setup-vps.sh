#!/bin/bash
set -e

# ============================================================
#  Gatums Gourmet — Setup inicial da VPS Locaweb
#
#  Instala Docker + Docker Compose, configura firewall (UFW),
#  cria swap para VPS com pouca RAM e garante SSH.
#
#  Chamado AUTOMATICAMENTE pelo GitHub Action no primeiro deploy
#  (se o Docker ainda não estiver instalado na VPS).
#
#  Também pode ser rodado manualmente:  bash deploy/setup-vps.sh
# ============================================================

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo ""
echo "=========================================="
echo "  Gatums Gourmet — Setup VPS Locaweb"
echo "=========================================="
echo ""

# === 1. Instalar Docker ===
if ! command -v docker &> /dev/null; then
    echo -e "${YELLOW}[1/4] Instalando Docker...${NC}"
    apt-get update
    apt-get install -y ca-certificates curl gnupg
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
    apt-get update
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
    systemctl enable docker
    systemctl start docker
    echo -e "${GREEN}Docker instalado ✅${NC}"
else
    echo -e "${GREEN}[1/4] Docker já instalado ✅${NC}"
fi

# === 2. Firewall ===
echo -e "${YELLOW}[2/4] Configurando firewall (UFW)...${NC}"
apt-get install -y ufw
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable
echo -e "${GREEN}Firewall configurado (portas 22, 80, 443) ✅${NC}"

# === 3. Swap (para VPS com 512MB RAM) ===
echo -e "${YELLOW}[3/4] Otimizando memória (swap)...${NC}"
if [ ! -f /swapfile ]; then
    fallocate -l 1G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
    echo -e "${GREEN}Swap 1GB criado ✅${NC}"
else
    echo -e "${GREEN}Swap já existe ✅${NC}"
fi
sysctl vm.swappiness=60
grep -q 'vm.swappiness' /etc/sysctl.conf || echo 'vm.swappiness=60' >> /etc/sysctl.conf
echo -e "${GREEN}Swappiness ajustado ✅${NC}"

# === 4. Garantir SSH ===
echo -e "${YELLOW}[4/4] Garantindo SSH...${NC}"
apt-get install -y openssh-server
systemctl enable ssh
systemctl start ssh
echo -e "${GREEN}SSH habilitado ✅${NC}"

echo ""
echo "=========================================="
echo -e "${GREEN}  Setup da VPS concluído! ✅${NC}"
echo "=========================================="
echo ""
echo "  Docker instalado e pronto para uso."
echo "  O GitHub Action criará o .env e fará o deploy automaticamente."
echo ""
echo "  Para deploy manual:"
echo "    1. Crie o .env:  cp .env.example .env && nano .env"
echo "    2. Suba os containers:  bash deploy/deploy-locaweb.sh"
echo ""
