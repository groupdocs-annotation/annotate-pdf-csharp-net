# Proves the sample runs on Linux: no Microsoft Office, no Adobe Acrobat, no X server.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/AnnotatePdf/AnnotatePdf.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /work

# Text rendering needs fonts; the runtime image ships without them.
RUN apt-get update \
 && apt-get install -y --no-install-recommends fontconfig fonts-liberation \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app /app
COPY samples /work/samples
ENTRYPOINT ["dotnet", "/app/annotate.dll"]
CMD ["info", "samples/quarterly-review.pdf"]
