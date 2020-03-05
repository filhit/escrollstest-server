sudo ./install-mono-repository.sh
sudo yum install mono-devel

# Register the Microsoft RedHat repository
curl https://packages.microsoft.com/config/rhel/7/prod.repo | sudo tee /etc/yum.repos.d/microsoft.repo

# Install PowerShell
sudo yum install -y powershell

curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install
rm -r aws
rm awscliv2.zip
aws configure

sudo cp backup-terraria-server-cron /etc/cron.d/

sudo cp terraria-server.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable terraria-server.service
