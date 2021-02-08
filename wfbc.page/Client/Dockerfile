#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["wfbc.page/Client/WFBC.Client.csproj", "wfbc.page/Client/"]
COPY ["wfbc.page/Shared/WFBC.Shared.csproj", "wfbc.page/Shared/"]
RUN dotnet restore "wfbc.page/Client/WFBC.Client.csproj"
COPY . .
WORKDIR /src/wfbc.page/Client
RUN dotnet build "WFBC.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WFBC.Client.csproj" -c Release -o /app/publish
 
FROM nginx:alpine AS final
# copy to server block
WORKDIR /var/www/wfbc.page/html
COPY --from=publish app/publish/wwwroot .
# server block location: /var/www/wfbc.page
# I think this would need to be a wfbc.page config file and copied to /etc/nginx/sites-available
# COPY wfbc.page/Client/nginx.conf /var/www/wfbc.page/nginx.conf