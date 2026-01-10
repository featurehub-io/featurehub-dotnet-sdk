FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

ARG Version

WORKDIR /FeatureHubSDK
COPY FeatureHubSDK/ /FeatureHubSDK/
COPY README.md /
RUN cd /FeatureHubSDK && \
     ls -la && dotnet build FeatureHubSDK.csproj

WORKDIR /web
COPY ToDoWebApi/ /web/
ENV ASPNETCORE_URLS=http://localhost:8099
ENV ASPNETCORE_ENVIRONMENT=Development
RUN cd /web && \
    dotnet build ToDoWebApi.csproj 
     
CMD ["dotnet", "run", "ToDoWebApi.csproj"]
