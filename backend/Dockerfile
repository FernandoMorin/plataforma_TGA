FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish PlataformaTGA.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Los documentos subidos (PDF) van en un volumen; la carpeta debe existir y ser del usuario "app".
RUN mkdir -p /app/App_Data/documentos && chown -R app:app /app/App_Data
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlataformaTGA.dll"]
