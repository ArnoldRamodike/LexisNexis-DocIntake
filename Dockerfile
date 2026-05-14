FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /
COPY *.sln .
COPY DocIntake.Api/*.csproj DocIntake.Api/
COPY DocIntake.Tests/*.csproj DocIntake.Tests/
RUN dotnet restore
COPY . .
RUN dotnet publish DocIntake.Api/DocIntake.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ConnectionStrings__BlobStorage=UseDevelopmentStorage=true
EXPOSE 80
ENTRYPOINT ["dotnet", "DocIntake.Api.dll"]