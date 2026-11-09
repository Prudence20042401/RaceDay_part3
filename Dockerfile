# Root Dockerfile builds the MVC front end. The API has its own Dockerfile.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay.MVC/RaceDay.MVC.csproj RaceDay.MVC/
RUN dotnet restore RaceDay.MVC/RaceDay.MVC.csproj
COPY RaceDay.MVC/ RaceDay.MVC/
RUN dotnet publish RaceDay.MVC/RaceDay.MVC.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.MVC.dll"]
