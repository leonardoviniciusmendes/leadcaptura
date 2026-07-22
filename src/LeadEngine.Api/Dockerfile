FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY LeadEngine.sln ./
COPY src/LeadEngine.Domain/LeadEngine.Domain.csproj src/LeadEngine.Domain/
COPY src/LeadEngine.Application/LeadEngine.Application.csproj src/LeadEngine.Application/
COPY src/LeadEngine.Infrastructure/LeadEngine.Infrastructure.csproj src/LeadEngine.Infrastructure/
COPY src/LeadEngine.Api/LeadEngine.Api.csproj src/LeadEngine.Api/
RUN dotnet restore src/LeadEngine.Api/LeadEngine.Api.csproj
COPY . .
RUN dotnet publish src/LeadEngine.Api/LeadEngine.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LeadEngine.Api.dll"]
