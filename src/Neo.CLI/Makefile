# Extract version from Directory.Build.props using shell command
VERSION := $(shell grep "<VersionPrefix>" ../Directory.Build.props | head -1 | sed -E 's/.*<VersionPrefix>([^<]+)<\/VersionPrefix>.*/\1/')

.PHONY: build

build:
ifeq ($(strip $(VERSION)),)
	$(error VersionPrefix not found in ../Directory.Build.props)
endif
	docker build --build-arg NEO_VERSION=$(VERSION) -t neo-node:$(VERSION) .