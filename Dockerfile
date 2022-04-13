# REQUIRES BUILDKIT TO BE ENABLED. https://docs.docker.com/develop/develop-images/build_enhancements/
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS demo-build-env
ENV NUGET_PACKAGES=/nuget
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY /sample/KerberosDemo/*.csproj ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet restore

# Copy everything else and build
COPY /sample/KerberosDemo/* ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet publish -c Debug -o out


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS sidecar-build-env
ENV NUGET_PACKAGES=/nuget
WORKDIR /app
COPY src/Directory.Build.* ./
COPY src/KerberosSidecar/*.csproj ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet restore

# Copy everything else and build
COPY src/KerberosSidecar/* ./
RUN --mount=type=cache,id=nuget,target=/nuget dotnet publish -c Debug -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt-get update
RUN apt install krb5-user -y
RUN mkdir -p /home/vcap/app/.krb5
ENV KRB5_CONFIG=/home/vcap/app/.krb5/krb5.conf
ENV KRB5CCNAME=/home/vcap/app/.krb5/krb5cc
ENV KRB5_KTNAME=/home/vcap/app/.krb5/service.keytab
ENV Logging__Console__FormatterName=simple
COPY --from=demo-build-env /app/launch.sh /
RUN chmod +x /launch.sh
COPY --from=demo-build-env /app/out /app
COPY --from=sidecar-build-env /app/out /krbsidecar

ENTRYPOINT ["/launch.sh"]
#ENTRYPOINT ["dotnet", "/app/KerberosDemo.dll"]