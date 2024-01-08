#!/bin/bash

# Set variables
ARCH="amd64";
PKGS_PATH="${DPKGS_PATH}/neo-cli_${NEO_VERSION}-1_${ARCH}";
TEMPLATE_PATH="./dpkg";

# Copy workflow deb template
cp -vpR ${TEMPLATE_PATH}/* ${PKGS_PATH}/

# Append deb package config
echo -e "Version: ${NEO_VERSION}" >> ${PKGS_PATH}/DEBIAN/control;
echo -e "Architecture: ${ARCH}" >> ${PKGS_PATH}/DEBIAN/control;

# Change Permissions
chmod -R a+rw ${PKGS_PATH}/srv/neo-node/dbs
chmod -R a+rw ${PKGS_PATH}/srv/neo-node/Plugins

# Clean up ".gitkeep"
rm -v ${PKGS_PATH}/srv/neo-node/dbs/.gitkeep
rm -v ${PKGS_PATH}/srv/neo-node/Plugins/.gitkeep

# Create "bin" Path
mkdir -vp ${PKGS_PATH}/usr/bin

# Build "deb" package
dpkg-deb --build ${DPKGS_PATH}

# Debug Output
dpkg-deb --info "${DPKGS_PATH}.deb"
