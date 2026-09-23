FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj và restore trước để tận dụng Docker layer cache
COPY ["demo1/demo1.csproj", "demo1/"]
RUN dotnet restore "./demo1/demo1.csproj"

# Copy toàn bộ source code và build
COPY . .
WORKDIR "/src/demo1"
RUN dotnet build "./demo1.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./demo1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Tạo sẵn thư mục logs & uploads và phân quyền đầy đủ cho non-root user 'app'
RUN mkdir -p /app/logs /app/uploads && chown -R app:app /app

USER app
ENTRYPOINT ["dotnet", "demo1.dll"]