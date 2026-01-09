FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

ARG Version

WORKDIR /app
ADD . /app
#RUN dotnet publish  -c Release -o out
#RUN dotnet test
RUN dotnet build /p:PackageVersion=$Version -c Release FeatureHubSDK.cspro && \
     dotnet pack /p:PackageVersion=$Version -c Release --no-restore --no-build -o /sln/artifacts FeatureHubSDK.cspro

# build with  docker build --build-arg Version=2.1.0 -t featurehub-dotnet-sdk .
# examine with: docker run --rm -it --entrypoint "/bin/sh" featurehub-dotnet-sdk -c /bin/bash
# deploy with docker run --rm  featurehub-dotnet-sdk --source https://api.nuget.org/v3/index.json --api-key MY-SECRET-KEY

