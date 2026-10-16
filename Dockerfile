FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Restaurante.Api/Restaurante.Api.csproj", "Restaurante.Api/"]
RUN dotnet restore "Restaurante.Api/Restaurante.Api.csproj"
COPY . .
WORKDIR "/src/Restaurante.Api"
RUN dotnet build "Restaurante.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Restaurante.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_HTTP_PORTS=10000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Restaurante.Api.dll"]