# MixedUp

Juego cooperativo de reparto (1-4 jugadores) hecho con **Unity 6 (6000.6.0f1), URP, Input System y uGUI/TextMeshPro**.
Recoge cajas con efectos (NORMAL, CALOR, ELÉCTRICA, HIELO, TÓXICA), llévalas al camión y resuelve el puzle de combinaciones
sin que nada explote. Idiomas: euskera, español e inglés.

## Cómo abrirlo

1. Abre la carpeta `MixedUp/` con Unity 6000.6.0f1.
2. Abre `Assets/Scenes/MainMenu.unity` y pulsa Play (el menú principal lleva al nivel `Level_Prototype`).
3. Si cambias scripts con campos serializados nuevos o el arte, vuelve a generar las escenas con
   **MixedUp > Build Prototype Scene**. Esa orden recrea materiales, prefabs, el nivel y el menú principal
   (los datos ya existentes en `Assets/_Project/Data` se conservan para no perder tus ajustes).

## Controles

| Acción | Teclado / ratón | Mando |
| --- | --- | --- |
| Moverse / correr / saltar | WASD (o flechas) / Shift / Espacio | Stick izquierdo |
| Cámara | Ratón | Stick derecho |
| Interactuar (coger, entregar, pasar caja) | E | — |
| Cambiar de caja | Q, rueda o 1-2 | — |
| Pausa y ajustes | Esc | — |

## Estructura (`MixedUp/Assets/_Project`)

| Carpeta | Qué hay |
| --- | --- |
| `Scripts/Core` | Estado del juego (`GameManager`), entradas (`GameInput`), idioma (`Localization`), preferencias (`GameSettings`) |
| `Scripts/Boxes` | `BoxData`, `BoxEffect` y los cuatro efectos (calor, eléctrico, hielo, tóxico). Todo son ScriptableObjects |
| `Scripts/Player` | Movimiento, inventario, estado, apariencia (`CharacterCustomization`), cajas en las manos (`CarriedBoxesView`) |
| `Scripts/World` | Cajas del mapa, camión, zonas peligrosas (agua, hielo, barro, fuego), tronco giratorio, setas saltarinas |
| `Scripts/Puzzle` | Reglas de combinación, estado del viaje, recompensa, cartera |
| `Scripts/UI` | HUD, menús, ajustes (`SettingsPanel`), menú principal (`MainMenu`), vista previa del personaje |
| `Editor` | Los constructores de escena (`PrototypeBuilder*.cs`), mallas procedurales (`LowPoly*.cs`, `CharacterMeshes`) |
| `Resources/Localization/strings.csv` | Todos los textos en `eu, es, en` |
| `Data` | Cajas, efectos, pedido, reglas y paleta del personaje (editables en el Inspector) |
| `Tests` | Tests EditMode y PlayMode (se juegan de verdad con teclado simulado) |

## Qué es fácil de cambiar

* **Cajas y efectos**: `Data/Boxes` y `Data/Effects` (daño, tiempos, resbalones...).
* **Reglas del puzle**: `Data/CombinationRules` (qué combinación es segura, peligrosa, explosión o game over).
* **Pedido**: `Data/Order_Prototype`.
* **Colores del personaje**: `Data/PlayerPalette` (5 pieles y 10 colores de ropa, los de tu dibujo).
* **Textos**: `Resources/Localization/strings.csv`.
* **Cajas en las manos**: componente `CarriedBoxesView` del prefab `Player` (tamaño, hueco entre cajas, posición del ancla).
* **Velocidad, salto, barro, caída**: componente `PlayerController`.

## Arte generado

El arte dibujado a mano se genera con scripts de Python (necesitan `pip install pillow numpy`):

```
python Tools/generate_character_art.py   # la cara del personaje
python Tools/generate_ui_art.py          # papel, carteles, sliders... y recortes de tus dibujos del Mixed_Up antiguo
```

Tras regenerarlo, vuelve a ejecutar **MixedUp > Build Prototype Scene**.

## Fuentes y licencias

* Fuente de la interfaz: **Bangers** (Vernon Adams), licencia SIL OFL 1.1.
* El repositorio es público: no se suben packs de terceros, solo arte propio.
