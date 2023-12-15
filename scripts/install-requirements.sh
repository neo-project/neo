#!/bin/bash

# Detect the operating system type
OS="$(uname -s)"
DISTRO=""

# Define installation functions
install_leveldb_ubuntu() {
    echo "Installing LevelDB on Ubuntu..."
    sudo apt-get update && sudo apt-get install -y libleveldb-dev
}

install_leveldb_fedora() {
    echo "Installing LevelDB on Fedora..."
    sudo dnf install -y leveldb-devel
}

install_leveldb_macos() {
    echo "Installing LevelDB on macOS..."
    brew install leveldb
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
                install_leveldb_ubuntu
                ;;
            fedora)
                install_leveldb_fedora
                ;;
            *)
                echo "Unsupported Linux distribution: $DISTRO"
                exit 1
                ;;
        esac
        ;;
    Darwin*)
        install_leveldb_macos
        ;;
    *)
        echo "Unsupported operating system: $OS"
        exit 1
        ;;
esac

echo "LevelDB installation completed"
