# syntax=docker/dockerfile:1

FROM node:20-bookworm-slim AS css
WORKDIR /src/DatKomp

COPY DatKomp/package.json DatKomp/package-lock.json ./
RUN npm ci

COPY DatKomp/tailwind.config.js ./
COPY DatKomp/Styles ./Styles
COPY DatKomp/Views ./Views
COPY DatKomp/wwwroot ./wwwroot

RUN npm run build:css


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY DatKomp/DatKomp.csproj DatKomp/
RUN dotnet restore DatKomp/DatKomp.csproj

COPY DatKomp/ DatKomp/
COPY --from=css /src/DatKomp/wwwroot/css/site.css DatKomp/wwwroot/css/site.css

RUN dotnet publish DatKomp/DatKomp.csproj -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DatKomp.dll"]
