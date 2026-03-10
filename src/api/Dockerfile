# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Anizaki.Api/Anizaki.Api.csproj", "src/Anizaki.Api/"]
COPY ["src/Anizaki.Application/Anizaki.Application.csproj", "src/Anizaki.Application/"]
COPY ["src/Anizaki.Domain/Anizaki.Domain.csproj", "src/Anizaki.Domain/"]
COPY ["src/Anizaki.Infrastructure/Anizaki.Infrastructure.csproj", "src/Anizaki.Infrastructure/"]

RUN dotnet restore "src/Anizaki.Api/Anizaki.Api.csproj"

COPY . .
RUN dotnet publish "src/Anizaki.Api/Anizaki.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Anizaki.Api.dll"]
