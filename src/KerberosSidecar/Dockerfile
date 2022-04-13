# REQUIRES BUILDKIT TO BE ENABLED. https://docs.docker.com/develop/develop-images/build_enhancements/
# build context = ../
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS sidecar-build-env
ENV NUGET_PACKAGES=/nuget
WORKDIR /app
COPY Directory.Build.* ./
COPY KerberosSidecar/*.csproj ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet restore

# Copy everything else and build
COPY ./KerberosSidecar/* ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet publish -c Debug -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update
RUN apt install krb5-user -y
ENV Logging__Console__FormatterName=simple
WORKDIR /app
COPY --from=sidecar-build-env /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:9090

#ENTRYPOINT ["/launch.sh"]
ENTRYPOINT ["dotnet", "KerberosSidecar.dll"]