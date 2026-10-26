# ÇAP/YDP Öğrenci Takip Otomasyonu — canlı ortam imajı (Render / Railway / Koyeb uyumlu)
# 1) Derleme aşaması
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS derleme
WORKDIR /kaynak
COPY CapYdp.Web/CapYdp.Web.csproj CapYdp.Web/
RUN dotnet restore CapYdp.Web/CapYdp.Web.csproj
COPY . .
RUN dotnet publish CapYdp.Web/CapYdp.Web.csproj -c Release -o /yayin /p:UseAppHost=false

# 2) Çalışma aşaması (yalnızca ASP.NET Core runtime; SDK ve kaynak kod imaja girmez)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS calisma
WORKDIR /uygulama
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Europe/Istanbul
COPY --from=derleme /yayin .
EXPOSE 8080
# Konteyner root olmayan kullanıcıyla çalışır (en az yetki ilkesi)
USER $APP_UID
ENTRYPOINT ["dotnet", "CapYdp.Web.dll"]
