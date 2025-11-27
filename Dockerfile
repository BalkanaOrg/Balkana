# BUILD STAGE
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file if it exists
COPY *.sln ./

# Copy only csproj first to take advantage of Docker caching
COPY Balkana/*.csproj Balkana/
RUN dotnet restore Balkana/Balkana.csproj

# Copy the rest of the app
COPY Balkana/ Balkana/

# Build & publish - don't treat warnings as errors
WORKDIR /src/Balkana
RUN dotnet publish -c Release -o /app/publish \
    -p:TreatWarningsAsErrors=false \
    --verbosity normal

# RUNTIME STAGE
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Balkana.dll"]
