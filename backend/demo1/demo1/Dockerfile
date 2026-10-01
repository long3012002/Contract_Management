FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# [CACHE LAYER] Copy .csproj và restore trước — layer này chỉ rebuild khi .csproj thay đổi
# → tận dụng Docker layer cache tối đa, tránh restore NuGet packages mỗi lần build
COPY ["demo1/demo1.csproj", "demo1/"]
RUN dotnet restore "./demo1/demo1.csproj"

# [SOURCE LAYER] Copy source code — layer này invalidate khi có thay đổi code C#
COPY . .
WORKDIR "/src/demo1"

# Bỏ lệnh "dotnet build" riêng vì dotnet publish đã tự gọi build bên trong
# → tránh compile 2 lần, tiết kiệm ~40-90 giây mỗi lần build
RUN dotnet publish "./demo1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Tạo thư mục logs & uploads với quyền đúng TRƯỚC khi copy app
# → tránh phải chạy chown -R sau COPY (chown sau COPY tạo layer thừa ~gấp đôi kích thước /app)
RUN mkdir -p /app/logs /app/uploads && chown -R app:app /app

# COPY kèm --chown trực tiếp — không tạo thêm layer chown riêng
COPY --chown=app:app --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "demo1.dll"]