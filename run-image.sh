#!/bin/sh
docker run --rm -it -e FEATUREHUB_CLIENT_API_KEY -e FEATUREHUB_EDGE_URL -p 8099:8099 dotnet-example:dev