# Build from the repository root:
#   docker build -f samples/UploadApi/Dockerfile -t uploadapi .
#   docker build -f samples/UploadApi/Dockerfile --build-arg VARIANT=-alpine -t uploadapi:alpine .
ARG VARIANT=""

FROM mcr.microsoft.com/dotnet/sdk:10.0${VARIANT} AS build
WORKDIR /src
COPY . .
RUN dotnet publish samples/UploadApi -c Release -o /app -p:MinVerSkip=true

# The runtime image has no extra packages: SkiaSharp.NativeAssets.Linux.NoDependencies covers glibc and musl.
FROM mcr.microsoft.com/dotnet/aspnet:10.0${VARIANT}
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UploadApi.dll"]
