# syntax=docker/dockerfile:1

ARG CONFIG=Release

########################
# AdminPanel build
########################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS adminpanel_build
ARG CONFIG
WORKDIR /src
COPY Utils/ Utils/
COPY AdminPanel/ AdminPanel/
WORKDIR /src/AdminPanel
RUN dotnet restore AdminPanel.csproj
RUN dotnet publish AdminPanel.csproj -c $CONFIG -o /out
RUN test -f /out/AdminPanel.dll \
 && test -f /out/AdminPanel.runtimeconfig.json \
 && test -f /out/AdminPanel.deps.json

########################
# Drawer build
########################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS drawer_build
ARG CONFIG
WORKDIR /src
COPY Utils/ Utils/
COPY Drawer/ Drawer/
WORKDIR /src/Drawer
RUN dotnet restore Drawer.csproj
RUN dotnet publish Drawer.csproj -c $CONFIG -o /out

########################
# Sorter build
########################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS sorter_build
ARG CONFIG
WORKDIR /src
COPY Utils/ Utils/
COPY Sorter/ Sorter/
WORKDIR /src/Sorter
RUN dotnet restore Sorter.csproj
RUN dotnet publish Sorter.csproj -c $CONFIG -o /out

########################
# Runtime base
########################
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

########################
# Final images
########################
FROM base AS adminpanel
COPY --from=adminpanel_build /out/ ./
ENV ADMINPANEL_URL=http://0.0.0.0:8082/
EXPOSE 8082
ENTRYPOINT ["dotnet","AdminPanel.dll"]

FROM base AS drawer
COPY --from=drawer_build /out/ ./
ENV DRAWER_URL=http://0.0.0.0:8080/
EXPOSE 8080
ENTRYPOINT ["dotnet","Drawer.dll"]

FROM base AS sorter
COPY --from=sorter_build /out/ ./
ENV SORTER_URL=http://0.0.0.0:8081/
EXPOSE 8081
ENTRYPOINT ["dotnet","Sorter.dll"]
