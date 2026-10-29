# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["AIStudyAssistant.API/AIStudyAssistant.API.csproj", "AIStudyAssistant.API/"]
RUN dotnet restore "AIStudyAssistant.API/AIStudyAssistant.API.csproj"

# Copy full source and publish release build
COPY . .
WORKDIR "/src/AIStudyAssistant.API"
RUN dotnet publish "AIStudyAssistant.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIStudyAssistant.API.dll"]
