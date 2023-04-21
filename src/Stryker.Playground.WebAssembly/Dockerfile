FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Stryker.Playground.WebAssembly/Stryker.Playground.WebAssembly.csproj", "Stryker.Playground.WebAssembly/"]
RUN dotnet restore "Stryker.Playground.WebAssembly/Stryker.Playground.WebAssembly.csproj"
COPY . .
WORKDIR "/src/Stryker.Playground.WebAssembly"
RUN dotnet build "Stryker.Playground.WebAssembly.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Stryker.Playground.WebAssembly.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stryker.Playground.WebAssembly.dll"]
