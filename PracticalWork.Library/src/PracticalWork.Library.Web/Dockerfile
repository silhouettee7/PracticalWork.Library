FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/PracticalWork.Library.Web/PracticalWork.Library.Web.csproj", "src/PracticalWork.Library.Web/"]
COPY ["src/PracticalWork.Library.Cache.Redis/PracticalWork.Library.Cache.Redis.csproj", "src/PracticalWork.Library.Cache.Redis/"]
COPY ["src/PracticalWork.Library/PracticalWork.Library.csproj", "src/PracticalWork.Library/"]
COPY ["src/PracticalWork.Library.Controllers/PracticalWork.Library.Controllers.csproj", "src/PracticalWork.Library.Controllers/"]
COPY ["src/PracticalWork.Library.Contracts/PracticalWork.Library.Contracts.csproj", "src/PracticalWork.Library.Contracts/"]
COPY ["src/PracticalWork.Library.Data.Minio/PracticalWork.Library.Data.Minio.csproj", "src/PracticalWork.Library.Data.Minio/"]
COPY ["src/PracticalWork.Library.Data.PostgreSql/PracticalWork.Library.Data.PostgreSql.csproj", "src/PracticalWork.Library.Data.PostgreSql/"]
RUN dotnet restore "src/PracticalWork.Library.Web/PracticalWork.Library.Web.csproj"
COPY . .
WORKDIR "/src/src/PracticalWork.Library.Web"
RUN dotnet build "PracticalWork.Library.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PracticalWork.Library.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PracticalWork.Library.Web.dll"]
