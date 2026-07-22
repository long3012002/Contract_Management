#!/bin/bash
# ==============================================================================
# CoopBank Server Setup & Deployment Script
# Target System: Ubuntu Server 22.04 LTS / RHEL 9
# ==============================================================================

set -e

echo "=================================================="
echo " Starting CoopBank App Server Initial Setup"
echo "=================================================="

# 1. Update system packages
echo "[1/5] Updating system packages..."
sudo apt update && sudo apt upgrade -y

# 2. Install Docker & Docker Compose if not installed
if ! command -v docker &> /dev/null; then
    echo "[2/5] Installing Docker Engine..."
    sudo apt install -y ca-certificates curl gnupg lsb-release
    sudo mkdir -p /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
      $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
    sudo apt update
    sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
    sudo usermod -aG docker $USER
fi

# 3. Configure Firewall (UFW) rules
echo "[3/5] Configuring UFW Firewall Matrix..."
sudo ufw allow 22/tcp       # SSH
sudo ufw allow 80/tcp       # HTTP
sudo ufw allow 443/tcp      # HTTPS
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw --force enable

# 4. Create Directory Hierarchy for App Logs & SSL Certificates
echo "[4/5] Creating application directory structure..."
sudo mkdir -p /etc/ssl/coopbank
sudo mkdir -p /var/log/qlda/backend
sudo mkdir -p /var/log/nginx
sudo mkdir -p /var/qlda/uploads

# 5. Summary & Instructions
echo "[5/5] Setup completed successfully!"
echo "--------------------------------------------------"
echo "NEXT STEPS FOR DEPLOYMENT:"
echo "1. Place SSL certificates in: /etc/ssl/coopbank/"
echo "   - /etc/ssl/coopbank/qlda.coopbank.vn.crt"
echo "   - /etc/ssl/coopbank/qlda.coopbank.vn.key"
echo "2. Copy Frontend dist files to project root dist folder."
echo "3. Run deployment command:"
echo "   docker compose -f docker-compose.prod.yml up -d --build"
echo "=================================================="
