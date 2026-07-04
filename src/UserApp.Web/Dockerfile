
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy full repo (keeps Dockerfile simple and reliable)
COPY . .

# restore using solution
RUN dotnet restore ./UserApp.sln

WORKDIR /src/src/UserApp.Web
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "UserApp.Web.dll"]
