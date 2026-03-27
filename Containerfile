FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY superserver.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 2007/tcp 2007/udp
EXPOSE 2009/tcp 2009/udp
EXPOSE 2013/tcp 2013/udp
EXPOSE 2019/tcp 2019/udp
EXPOSE 8080/tcp

USER $APP_UID
ENTRYPOINT ["dotnet", "superserver.dll"]
