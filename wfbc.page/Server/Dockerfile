#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["WFBC/Server/WFBC.Server.csproj", "WFBC/Server/"]
COPY ["WFBC/Client/WFBC.Client.csproj", "WFBC/Client/"]
COPY ["WFBC/Shared/WFBC.Shared.csproj", "WFBC/Shared/"]
RUN dotnet restore "WFBC/Server/WFBC.Server.csproj"
COPY . .
WORKDIR "/src/WFBC/Server"
RUN dotnet build "WFBC.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WFBC.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WFBC.Server.dll"]