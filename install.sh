#!/bin/bash
set -e

# Super Server Installation Script

INSTALL_DIR="/opt/superserver"
CONFIG_DIR="/etc/superserver"
SERVICE_USER="superserver"
SERVICE_GROUP="superserver"

echo "Installing Super Server..."

# Check for root privileges
if [ "$EUID" -ne 0 ]; then
    echo "Error: This script must be run as root"
    exit 1
fi

# Create service user and group
if ! getent group "$SERVICE_GROUP" > /dev/null 2>&1; then
    echo "Creating group: $SERVICE_GROUP"
    groupadd --system "$SERVICE_GROUP"
fi

if ! getent passwd "$SERVICE_USER" > /dev/null 2>&1; then
    echo "Creating user: $SERVICE_USER"
    useradd --system --no-create-home --shell /usr/sbin/nologin -g "$SERVICE_GROUP" "$SERVICE_USER"
fi

# Create directories
echo "Creating directories..."
mkdir -p "$INSTALL_DIR"
mkdir -p "$CONFIG_DIR"

# Build the application
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "Building application..."

# Run dotnet publish as the original user to avoid creating root-owned files in source directory
if [ -n "$SUDO_USER" ]; then
    # Create temp dir as original user so they can write to it
    BUILD_DIR=$(sudo -u "$SUDO_USER" mktemp -d)
    sudo -u "$SUDO_USER" dotnet publish "$SCRIPT_DIR" -c Release -o "$BUILD_DIR"
else
    BUILD_DIR=$(mktemp -d)
    dotnet publish "$SCRIPT_DIR" -c Release -o "$BUILD_DIR"
fi

# Copy binaries
echo "Copying binaries to $INSTALL_DIR..."
cp -r "$BUILD_DIR/"* "$INSTALL_DIR/"
rm -rf "$BUILD_DIR"

# Copy configuration
echo "Copying configuration to $CONFIG_DIR..."
if [ ! -f "$CONFIG_DIR/appsettings.json" ]; then
    cp "$SCRIPT_DIR/appsettings.json" "$CONFIG_DIR/"
else
    echo "Configuration file already exists, skipping..."
fi

# Create symlink for config in install directory
ln -sf "$CONFIG_DIR/appsettings.json" "$INSTALL_DIR/appsettings.json"

# Set permissions
echo "Setting permissions..."
chown -R "$SERVICE_USER:$SERVICE_GROUP" "$INSTALL_DIR"
chown -R "$SERVICE_USER:$SERVICE_GROUP" "$CONFIG_DIR"
chmod 755 "$CONFIG_DIR"
chmod 755 "$INSTALL_DIR"
chmod 644 "$CONFIG_DIR/appsettings.json"
chmod 755 "$INSTALL_DIR/superserver"

# Install systemd unit
echo "Installing systemd service..."
cp "$SCRIPT_DIR/superserver.service" /etc/systemd/system/
chmod 644 /etc/systemd/system/superserver.service

# Reload systemd
echo "Reloading systemd..."
systemctl daemon-reload

# Enable service
echo "Enabling service..."
systemctl enable superserver

echo ""
echo "Installation complete!"
echo ""
echo "To start the service:"
echo "  sudo systemctl start superserver"
echo ""
echo "To check status:"
echo "  sudo systemctl status superserver"
echo ""
echo "To view logs:"
echo "  sudo journalctl -u superserver -f"
echo ""
echo "Configuration file: $CONFIG_DIR/appsettings.json"
