.PHONY: all build test restore \
        build-sdk build-otel \
        test-sdk test-otel \
        pack docker docker-run

# ---------------------------------------------------------------------------
# Aggregates
# ---------------------------------------------------------------------------

all: build test

restore:
	dotnet restore

build: restore
	dotnet build FeatureHubSDK.sln

test:
	dotnet test FeatureHubSDK.sln

# ---------------------------------------------------------------------------
# Fine-grained: individual projects
# ---------------------------------------------------------------------------

build-sdk: restore
	dotnet build FeatureHubSDK/FeatureHubSDK.csproj

build-otel: restore
	dotnet build FeatureHubUsageOpenTelemetry/FeatureHubUsageOpenTelemetry.csproj

test-sdk:
	dotnet test FeatureHubTest/FeatureHubTest.csproj

test-otel:
	dotnet test FeatureHubUsageOpenTelemetryTest/FeatureHubUsageOpenTelemetryTest.csproj

# ---------------------------------------------------------------------------
# Pack
# ---------------------------------------------------------------------------

PACKAGE_VERSION ?= 0.0.0-local

pack: build
	cd FeatureHubSDK && dotnet pack /p:PackageVersion=$(PACKAGE_VERSION) --configuration Release --no-build

# ---------------------------------------------------------------------------
# Docker
# ---------------------------------------------------------------------------

docker:
	docker build -t featurehub/dotnet-sdk-todo .

docker-run: docker
	docker run -e FEATUREHUB_CLIENT_API_KEY -e FEATUREHUB_EDGE_URL -p 8099:8099 featurehub/dotnet-sdk-todo
