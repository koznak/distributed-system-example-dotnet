FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Services/Orders/Orders.Api/Orders.Api.csproj", "src/Services/Orders/Orders.Api/"]
RUN dotnet restore "src/Services/Orders/Orders.Api/Orders.Api.csproj"
COPY . .
RUN dotnet publish "src/Services/Orders/Orders.Api/Orders.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
