#!/bin/bash

# Detect the operating system type
OS="$(uname -s)"
DISTRO=""

# Define installation functions
install_requirements_ubuntu() {
    echo "Installing requirements and dependencies on Ubuntu..."
    sudo apt-get update
    sudo apt-get install -y libleveldb-dev sqlite3 libsqlite3-dev libunwind8-dev
}

install_requirements_fedora() {
    echo "Installing requirements and dependencies on Fedora..."
    sudo dnf install -y leveldb-devel sqlite sqlite-devel libunwind-devel
}

install_requirements_opensuse() {
    echo "Installing requirements and dependencies on openSUSE..."
    sudo zypper install -y libleveldb-devel sqlite3 libsqlite3-devel libunwind-devel
}

install_requirements_macos() {
    echo "Installing requirements and dependencies on macOS..."
    brew install leveldb sqlite3 libunwind
}

# Detect Linux distribution
detect_linux_distro() {
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        DISTRO=$ID
    fi
}

# Execute the appropriate installation function based on the OS
case $OS in
    Linux*)
        detect_linux_distro
        case $DISTRO in
            ubuntu)
                install_requirements_ubuntu
                ;;
            fedora)
                install_requirements_fedora
                ;;
            opensuse*|suse|sles)
                install_requirements_opensuse
                ;;
            *)
                echo "Unsupported Linux distribution: $DISTRO"
                exit 1
                ;;
        esac
        ;;
    Darwin*)
        install_requirements_macos
        ;;
    *)
        echo "Unsupported operating system: $OS"
        exit 1
        ;;
esac

echo "Requirements and dependencies installation completed"
