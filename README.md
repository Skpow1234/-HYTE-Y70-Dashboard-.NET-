# HYTE Y70 Dashboard (.NET)

Cliente **nativo** (WPF) para el monitor **HYTE Y70 Touch Infinite**.  
Sin Electron, sin Nexus, sin Chromium.

Doble clic: `publish\HyteY70.exe`

## Peso y objetivo

| | Aprox. |
|--|--|
| `.exe` (framework-dependent) | **~190 KB** |
| RAM en uso | **~40–90 MB** privados típicos |
| Referencia a evitar | Nexus / Electron (cientos de MB + Chromium) |

**Objetivo del proyecto:** nativo, rápido y lo más liviano posible. Misma filosofía que Thermalright Vision: config + bandeja, sin UI pesada.

Requiere [**.NET 8 Desktop Runtime**](https://dotnet.microsoft.com/download/dotnet/8.0).

---

## Cómo modificar la config

1. Abre la carpeta `publish\` (o clic derecho en la bandeja → **Abrir carpeta de config**).
2. Edita `config.json` con el Bloc de notas, VS Code, etc.
3. Guarda el archivo.
4. Si `watchConfig` es `true`, la app recarga sola en ~0,3 s. Si no, bandeja → **Recargar config**.

Reglas del JSON:

- Comillas dobles en claves y textos: `"gif": "fondo.gif"`.
- Booleanos: `true` / `false` (sin comillas).
- Números sin comillas: `"fps": 20`.
- Sin coma tras el último campo.
- Rutas relativas = respecto a la carpeta del `.exe` (`publish\`).

Ejemplo solo estadísticas:

```json
{
  "mode": "stats",
  "fps": 20,
  "showClock": true,
  "showCpu": true,
  "showGpu": true,
  "showRam": true
}
```

Ejemplo solo GIF a pantalla completa (sin tarjetas encima):

```json
{
  "mode": "auto",
  "fps": 20,
  "fpsIdle": 20,
  "gif": "media/fondo.gif",
  "fit": "contain",
  "zoom": 1.0,
  "rotate": 0,
  "overlayStats": false
}
```

Ejemplo GIF + barra fina de stats:

```json
{
  "mode": "auto",
  "gif": "media/fondo.gif",
  "overlayStats": true
}
```

---

## Todos los parámetros

### Modo y contenido

| Parámetro | Tipo | Default | Qué hace |
|-----------|------|---------|----------|
| `mode` | texto | `"auto"` | `auto` = sin media → estadísticas; con GIF/imagen → solo media. `stats` = siempre estadísticas. `media` = siempre media (si el archivo existe). |
| `gif` | texto | `""` | Ruta a GIF o imagen. Relativa al `.exe` o absoluta. Vacío = no hay media. |
| `media` | texto | `""` | Alias de `gif` si `gif` está vacío. |

### Rendimiento

| Parámetro | Tipo | Default | Qué hace |
|-----------|------|---------|----------|
| `fps` | número | `20` | Tope de FPS cuando el contenido cambia. En la Y70 suele bastar `15–30`. |
| `fpsIdle` | número | `2` | FPS cuando el fotograma del GIF no cambia. Para animación más fluida, iguala a `fps`. `0` = esperar al siguiente frame del GIF. |

### Orientación y encaje

| Parámetro | Tipo | Default | Qué hace |
|-----------|------|---------|----------|
| `rotate` | número | `0` | `0`, `90`, `180`, `270`. |
| `flipHorizontal` | bool | `false` | Espejo horizontal. |
| `flipVertical` | bool | `false` | Espejo vertical. |
| `fit` | texto | `"contain"` | `contain` = cabe entero (bordes posibles). `cover` = llena y recorta. |
| `zoom` | número | `1.0` | Multiplicador 0.1–5.0 sobre el encaje. |
| `letterboxColor` | texto | `"#000000"` | Color de bordes en `contain` (`#RRGGBB` o `r,g,b`). |

### Estadísticas

| Parámetro | Tipo | Default | Qué hace |
|-----------|------|---------|----------|
| `showClock` | bool | `true` | Reloj (en modo stats o en overlay). |
| `showCpu` | bool | `true` | CPU (carga / GHz; °C si HWiNFO Shared Memory está activo). |
| `showGpu` | bool | `true` | GPU (temp / hotspot / uso). |
| `showRam` | bool | `true` | RAM. |
| `overlayStats` | bool | `false` | Si hay media y es `true`, dibuja una **barra fina** de stats encima del GIF/imagen (sin las tarjetas grandes). |

Temp CPU: HWiNFO → Settings → **Shared Memory Support**.

### Arranque

| Parámetro | Tipo | Default | Qué hace |
|-----------|------|---------|----------|
| `startHidden` | bool | `false` | Si `true`, no muestra la ventana en la Y70 al arrancar (sigue en bandeja). |
| `runAtLogin` | bool | `false` | Registrar inicio con Windows al cargar la config. También desde la bandeja. |
| `watchConfig` | bool | `true` | Recarga automática al guardar `config.json`. |

---

## Bandeja del sistema

- **Mostrar en Y70** / **Ocultar** (Esc también oculta; el proceso sigue)
- **Recargar config**
- **Abrir carpeta de config**
- **Iniciar / no iniciar con Windows**
- **Salir** (cierra del todo)

La app detecta el monitor `HYTE Y70ti` (`RTK1234`) y se abre a pantalla completa ahí.

---

## Publicar de nuevo

```bat
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

Tras publicar, revisa que tu `publish\config.json` no se haya sobrescrito con valores por defecto del proyecto.
