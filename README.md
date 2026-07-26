# NIDO CERO — GameJam2026

Vertical slice 2.5D ortográfico para la temática **TODO TIENE UN COSTO**.
El prototipo usa geometría 3D básica como blockout y está preparado para
recibir sprites sobre sus caras sin sustituir colisiones ni físicas.

## Versión y arranque

- Unity: `6000.3.14f1`
- Paquete MCP: `com.coplaydev.unity-mcp` fijado en `v10.0.0`
- Primera escena: `Assets/_Game/Scenes/00_Launcher.unity`
- Regeneración del prototipo: `Nido Cero > Build Complete Prototype`

## Escenas

- `00_Launcher`: menú, controles y acceso al juego.
- `01_CinematicIntro`: contexto narrativo con subtítulos y omisión.
- `02_MainScene`: cuatro pisos descendentes, decisiones, llaves, rescate y jefe.
- `03_CinematicEnd`: sacrificio final y retorno al launcher.
- `99_DevValidation`: validación interna, excluida del build final.

## Controles

- `A / D`: caminar
- `Shift`: correr
- `Espacio`: saltar / omitir cinemática
- `Mouse`: apuntar
- `Click izquierdo`: disparar
- `1 / 2 / 3` o click: elegir carta
- `Esc`: pausa / omitir cinemática

## Datos editables

Los stats, elementos, cartas, enemigos y pisos son ScriptableObjects en
`Assets/_Game/Generated/Data`. El estado de la partida se guarda por separado
en `RunState`; una carta elegida aplica ganancia, costo y elemental, y las tres
cartas mostradas se retiran de esa partida.

## Pruebas

El proyecto contiene pruebas EditMode y PlayMode en `Assets/_Game/Tests`.
Validan el catálogo, la fórmula elemental, límites de stats, build settings,
las cinco escenas sin scripts perdidos, arranque de runtime y estabilidad
física del blockout.

## MCP

El menú `Nido Cero > MCP` permite configurar todos los clientes detectados,
arrancar el servidor HTTP local y reconectar únicamente el puente. El endpoint
local configurado para Codex es `http://127.0.0.1:8080/mcp`.
