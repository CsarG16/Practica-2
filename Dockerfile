# 1. Etapa de compilación (Build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copiar el archivo del proyecto y restaurar dependencias
COPY *.csproj .
RUN dotnet restore

# Copiar todo el resto del código y compilar la aplicación
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# 2. Etapa de ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copiamos la app compilada desde la etapa anterior
COPY --from=build /app .

# Exponemos el puerto estándar donde Render buscará la app
EXPOSE 8080

# Comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "Practica_2.dll"]
