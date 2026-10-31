# ---------- Etapa 1: compilar ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY BemEstarOnline.Api.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# ---------- Etapa 2: imagem final, só com o necessário para rodar ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    TZ=America/Sao_Paulo
EXPOSE 8080
# Pasta das fotos dos produtos com permissão de escrita para o usuário da aplicação.
RUN mkdir -p /app/wwwroot/imagens/produtos && chown -R app:app /app/wwwroot
# Roda com um usuário sem privilégios de administrador (boa prática de segurança).
USER app
ENTRYPOINT ["dotnet", "BemEstarOnline.Api.dll"]
