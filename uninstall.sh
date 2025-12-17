#!/bin/bash
set -e

# Super Server Uninstallation Script

INSTALL_DIR="/opt/superserver"
CONFIG_DIR="/etc/superserver"
SERVICE_USER="superserver"
SERVICE_GROUP="superserver"

echo "Uninstalling Super Server..."

# Check for root privileges
if [ "$EUID" -ne 0 ]; then
    echo "Error: This script must be run as root"
    exit 1
fi

# Stop and disable service
if systemctl is-active --quiet superserver 2>/dev/null; then
    echo "Stopping service..."
    systemctl stop superserver
fi

if systemctl is-enabled --quiet superserver 2>/dev/null; then
    echo "Disabling service..."
    systemctl disable superserver
fi

# Remove systemd unit
if [ -f /etc/systemd/system/superserver.service ]; then
    echo "Removing systemd unit..."
    rm -f /etc/systemd/system/superserver.service
    systemctl daemon-reload
fi

# Remove binaries
if [ -d "$INSTALL_DIR" ]; then
    echo "Removing binaries from $INSTALL_DIR..."
    rm -rf "$INSTALL_DIR"
fi

# Ask about configuration
if [ -d "$CONFIG_DIR" ]; then
    read -p "Remove configuration directory $CONFIG_DIR? [y/N] " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "Removing configuration..."
        rm -rf "$CONFIG_DIR"
    else
        echo "Keeping configuration directory."
    fi
fi

# Ask about user/group
if getent passwd "$SERVICE_USER" > /dev/null 2>&1; then
    read -p "Remove service user '$SERVICE_USER'? [y/N] " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "Removing user: $SERVICE_USER"
        userdel "$SERVICE_USER"
    else
        echo "Keeping service user."
    fi
fi

if getent group "$SERVICE_GROUP" > /dev/null 2>&1; then
    # Only remove group if user was removed or doesn't exist
    if ! getent passwd "$SERVICE_USER" > /dev/null 2>&1; then
        read -p "Remove service group '$SERVICE_GROUP'? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            echo "Removing group: $SERVICE_GROUP"
            groupdel "$SERVICE_GROUP"
        else
            echo "Keeping service group."
        fi
    fi
fi

echo ""
echo "Uninstallation complete!"
