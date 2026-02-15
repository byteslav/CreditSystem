FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/CreditSystem.Api/CreditSystem.Api.csproj", "src/CreditSystem.Api/"]
COPY ["src/CreditSystem.Application/CreditSystem.Application.csproj", "src/CreditSystem.Application/"]
COPY ["src/CreditSystem.Infrastructure/CreditSystem.Infrastructure.csproj", "src/CreditSystem.Infrastructure/"]
COPY ["src/CreditSystem.Domain/CreditSystem.Domain.csproj", "src/CreditSystem.Domain/"]

RUN dotnet restore "src/CreditSystem.Api/CreditSystem.Api.csproj"

COPY src/ ./src/
WORKDIR /src/src/CreditSystem.Api
RUN dotnet publish "CreditSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CreditSystem.Api.dll"]