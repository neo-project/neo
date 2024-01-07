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
