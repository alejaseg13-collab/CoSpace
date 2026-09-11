FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/CoSpace.Domain/CoSpace.Domain.csproj src/CoSpace.Domain/
COPY src/CoSpace.Application/CoSpace.Application.csproj src/CoSpace.Application/
COPY src/CoSpace.Infrastructure/CoSpace.Infrastructure.csproj src/CoSpace.Infrastructure/
COPY src/CoSpace.Api/CoSpace.Api.csproj src/CoSpace.Api/
RUN dotnet restore src/CoSpace.Api/CoSpace.Api.csproj
COPY src/ src/
RUN dotnet publish src/CoSpace.Api/CoSpace.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoSpace.Api.dll"]