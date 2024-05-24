#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["wfbc.page/Server/WFBC.Server.csproj", "wfbc.page/Server/"]
COPY ["wfbc.page/Client/WFBC.Client.csproj", "wfbc.page/Client/"]
COPY ["wfbc.page/Shared/WFBC.Shared.csproj", "wfbc.page/Shared/"]
RUN dotnet restore "wfbc.page/Server/WFBC.Server.csproj"
COPY . .
WORKDIR "/src/wfbc.page/Server"
RUN dotnet build "WFBC.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WFBC.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WFBC.Server.dll"]