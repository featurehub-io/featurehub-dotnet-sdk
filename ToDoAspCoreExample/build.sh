#!/usr/bin/env bash
#
# Generated by: https://openapi-generator.tech
#

dotnet restore ToDoAspCoreExample.csproj && \
    dotnet build ToDoAspCoreExample.csproj && \
    echo "Now, run the following to start the project: dotnet run -p ToDoAspCoreExample.csproj"
