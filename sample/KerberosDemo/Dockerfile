# REQUIRES BUILDKIT TO BE ENABLED. https://docs.docker.com/develop/develop-images/build_enhancements/
# build context = .
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS demo-build-env
ENV NUGET_PACKAGES=/nuget
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./*.csproj ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet restore

# Copy everything else and build
COPY ./* ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet publish -c Debug -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update
RUN apt install krb5-user -y
ENV Logging__Console__FormatterName=simple
WORKDIR /app
COPY --from=demo-build-env /app/out ./

ENTRYPOINT ["dotnet", "KerberosDemo.dll"]