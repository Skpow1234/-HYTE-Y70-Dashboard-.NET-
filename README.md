# HYTE Y70 Dashboard (.NET)

Ejecutable nativo (WPF). Doble clic en `publish\HyteY70.exe`.

## Modos

- `mode: auto` — sin media → estadísticas; con GIF/imagen → solo media
- `mode: stats` / `mode: media`

## Config (`config.json`)

```json
{
  "mode": "auto",
  "fps": 20,
  "gif": "",
  "rotate": 0,
  "flipHorizontal": false,
  "flipVertical": false,
  "fit": "contain",
  "zoom": 1.0,
  "showClock": true,
  "showCpu": true,
  "showGpu": true,
  "showRam": true,
  "startHidden": false,
  "runAtLogin": false
}
```

## Bandeja

- Mostrar / ocultar en la Y70
- Recargar config
- Abrir carpeta de config
- Iniciar con Windows
- Salir (Esc solo oculta)

## Publicar

```bat
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```
