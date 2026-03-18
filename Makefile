docker:
	docker build -t featurehub/dotnet-sdk-todo .

docker-run: docker
	docker run -e FEATUREHUB_CLIENT_API_KEY -e FEATUREHUB_EDGE_URL -p 8099:8099 featurehub/dotnet-sdk-todo