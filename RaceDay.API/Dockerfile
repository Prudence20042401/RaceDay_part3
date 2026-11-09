# Build and publish the ASP.NET Core API.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.API/RaceDay.API.csproj RaceDay.API/
RUN dotnet restore RaceDay.API/RaceDay.API.csproj
COPY RaceDay.API/ RaceDay.API/
RUN dotnet publish RaceDay.API/RaceDay.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
