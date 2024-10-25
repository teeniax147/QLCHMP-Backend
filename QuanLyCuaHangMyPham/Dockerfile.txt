# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["QuanLyCuaHangMyPham.csproj", "./"]
RUN dotnet restore "QuanLyCuaHangMyPham.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "QuanLyCuaHangMyPham.csproj" -c Release -o /app/build

# Publish the app to the app directory
FROM build AS publish
RUN dotnet publish "QuanLyCuaHangMyPham.csproj" -c Release -o /app/publish

# Copy the published app and run it
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuanLyCuaHangMyPham.dll"]