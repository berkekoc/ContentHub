# ContentHub API imajı (.NET 10). Render / herhangi bir Docker host için.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ContentHub.sln
RUN dotnet publish src/Bootstrap/ContentHub.Api/ContentHub.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# Npgsql'in GSSAPI/Kerberos yolu için gerekli kütüphane — slim runtime imajında yoktur
# (aksi halde: 'Cannot load library libgssapi_krb5.so.2' → açılışta çökme).
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ContentHub.Api.dll"]
