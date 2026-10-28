# HYTE Y70 Dashboard (.NET)

Cliente **nativo** (WPF) para el monitor **HYTE Y70 Touch Infinite**.  
Sin Electron, sin Nexus, sin Chromium.

Doble clic: `publish\HyteY70.exe`

## Peso y objetivo

| | Aprox. |
|--|--|
| `.exe` (framework-dependent) | **~190 KB** |
| RAM en uso | **~40–90 MB** privados típicos |
| Referencia a evitar | Nexus / Electron (cientos de MB) |

**Objetivo:** nativo, rápido y lo más liviano posible.

Requiere [**.NET 8 Desktop Runtime**](https://dotnet.microsoft.com/download/dotnet/8.0).

## Modos

- `mode: auto` / `stats` / `media`

## Config (`config.json`)

```json
{
  "mode": "auto",
  "fps": 20,
  "fpsIdle": 2,
  "gif": "",
  "rotate": 0,
  "fit": "contain",
  "zoom": 1.0,
  "letterboxColor": "#000000",
  "showClock": true,
  "showCpu": true,
  "showGpu": true,
  "showRam": true,
  "overlayStats": false,
  "watchConfig": true,
  "runAtLogin": false
}
```

- `overlayStats`: barra fina de stats encima del GIF/imagen (opt-in)
- `fpsIdle` / `letterboxColor` / `watchConfig`: igual que Thermalright
- Temp CPU vía HWiNFO Shared Memory si está disponible

## Bandeja

- Mostrar / ocultar / Recargar config / Carpeta / Inicio con Windows / Salir

## Publicar

```bat
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```
