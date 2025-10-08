# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source code and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# ✅ Set environment for Swagger
ENV ASPNETCORE_ENVIRONMENT=Development

# ✅ Expose the port your app runs on
EXPOSE 8080

# ✅ Run the app
ENTRYPOINT ["dotnet", "Backend.dll"]
