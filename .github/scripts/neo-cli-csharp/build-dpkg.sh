#!/bin/bash
# Copyright (C) 2015-2024 The Neo Project.
#
# build-dpkg-amd64.sh file belongs to the neo project and is free
# software distributed under the MIT software license, see the
# accompanying file LICENSE in the main directory of the
# repository or http://www.opensource.org/licenses/mit-license.php
# for more details.
#
# Redistribution and use in source and binary forms with or without
# modifications are permitted.

# Set variables
ARCH=${1};
PKGS_PATH="${DPKGS_PATH}/neo-cli-${OS_RUNTIME}_${NEO_VERSION}-1_${ARCH}";
TEMPLATE_PATH="./dpkg";

# Make directory
mkdir -p ${PKGS_PATH};

# Copy workflow deb template
cp -vpR ${TEMPLATE_PATH}/* ${PKGS_PATH}/;

# Create "bin" Path
mkdir -vp ${PKGS_PATH}/usr/bin;

# Change permissions
chmod -R 755 ${PKGS_PATH}/;
chmod -R a+rw ${PKGS_PATH}/srv/neo-node/dbs;
chmod -R a+rw ${PKGS_PATH}/srv/neo-node/Plugins;

# Clean up ".gitkeep"
rm -v ${PKGS_PATH}/srv/neo-node/dbs/.gitkeep;
rm -v ${PKGS_PATH}/srv/neo-node/Plugins/.gitkeep;

# Append deb package config
echo -e "Version: ${NEO_VERSION}" >> ${PKGS_PATH}/DEBIAN/control;
echo -e "Architecture: ${ARCH}" >> ${PKGS_PATH}/DEBIAN/control;

# Create shortcut to "neo-cli" binary file
ln -sv /srv/neo-node/neo-cli ${PKGS_PATH}/usr/bin/neo-cli;

# Copy "neo-cli" Binaries
cp -vp ${BIN_RUNTIME_PATH}/* ${PKGS_PATH}/srv/neo-node/;

# Build "deb" package
dpkg-deb --build ${PKGS_PATH};
status=${?};

if [ ${status} -eq 0 ]; then
  # Debug Output
  dpkg-deb --info "${PKGS_PATH}.deb";
fi

echo "PKGS_PATH=${PKGS_PATH}" >> ${GITHUB_ENV}

exit ${status};
