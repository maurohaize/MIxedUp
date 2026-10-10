# MixedUp

Juego cooperativo de reparto (1-4 jugadores) hecho con **Unity 6 (6000.6.0f1), URP, Input System y uGUI/TextMeshPro**.
Recoge cajas con efectos (NORMAL, CALOR, ELÉCTRICA, HIELO, TÓXICA), llévalas al camión y resuelve el puzle de combinaciones
sin que nada explote. Idiomas: euskera, español e inglés.

## Cómo abrirlo

1. Abre la carpeta `MixedUp/` con Unity 6000.6.0f1.
2. Abre `Assets/Scenes/MainMenu.unity` y pulsa Play (el menú principal lleva al nivel `Level_Prototype`).
3. Si cambias scripts con campos serializados nuevos o el arte, vuelve a generar las escenas con
   **MixedUp > Build Prototype Scene**. Esa orden recrea materiales, prefabs, los niveles, el lobby y el menú principal
   (los datos ya existentes en `Assets/_Project/Data` se conservan para no perder tus ajustes).

## Controles

| Acción | Teclado / ratón | Mando |
| --- | --- | --- |
| Moverse / correr / saltar | WASD (o flechas) / Shift / Espacio | Stick izquierdo |
| Cámara | Ratón | Stick derecho |
| Interactuar (coger, entregar, pasar caja) | E | Botón oeste |
| Quitar todas las cajas a un compañero | F | Botón este |
| Agacharse | C o Ctrl izquierdo | Pulsar stick derecho |
| Empujar a otro jugador | G o clic izquierdo | Botón superior derecho |
| Abrazar (cura a los dos; con más jugadores es la única forma de recuperar vida) | H | Botón superior izquierdo |
| Espectar a otro jugador (estando muerto) | E | Botón oeste |
| Cambiar de caja | Q, rueda o 1-2 | Botón norte |
| Pausa y ajustes | Esc | — |

Al morir, las cajas que llevabas caen al suelo para que tu equipo las recoja, y puedes ver jugar a tus compañeros. La partida solo termina cuando caen todos.

## Modos de partida

Se eligen en el menú principal (tarjeta de la esquina) o los elige el anfitrión en una sala:

| Modo | Pedido | Cajas | Tiempo |
| --- | --- | --- | --- |
| Reparto clásico | El del mapa (6 cajas) | Donde las diseñó el mapa | Sin límite |
| Entrega exprés | 3 cajas al azar | Sitios al azar | 3 min |
| Pedido gigante | 9 cajas al azar | Sitios al azar | 10 min |
| Sorpresa | 5-7 cajas al azar | Sitios al azar | 7 min |
| Entrega nocturna | El del mapa | Como el clásico, de noche (las cajas brillan) | Sin límite |
| Desafío | 5 cajas al azar | Solo sitios difíciles: torre de saltos, túnel, repisas con setas, tronco giratorio, rampa de hielo | 8 min |

Todos se pueden ganar (los pedidos aleatorios solo se aceptan si existe una colocación segura, `PuzzleSolver`) y perder
(morir, quedarte sin tiempo o que explote el camión).

## Mapas

Se eligen en el menú principal (tarjeta "MAPA", partida en solitario) o los elige el anfitrión en el lobby:

| Mapa | Escena | De qué va |
| --- | --- | --- |
| Pradera del reparto | `Level_Prototype` | El mapa original: río, cueva, colina helada, tronco giratorio |
| Cumbre helada | `Level_Summit` | Nivel superior: lago helado enorme (todo resbala), campamento con hoguera, pilón eléctrico, rampa de hielo, torre, túnel y acantilado de setas |
| Puerto al anochecer | `Level_Harbour` | Muelles sobre una bahía poco profunda, balsa a la isla, barcaza, pila de cajas y faro. Agua y electricidad muy cerca: ideal para "Entrega nocturna" y "Desafío" |

Todos los mapas funcionan con todos los modos de partida. Se generan con **MixedUp > Build Prototype Scene**
(`PrototypeBuilder.Maps.cs` es el andamio común; cada mapa solo rellena su terreno, hazards y puntos de cajas).

## Multijugador online

**Multijugador** en el menú: crea una sala o únete a la de un amigo. Hay dos formas de conectarse (botón "Conexión" de la sala):

| Conexión | Qué se comparte | Qué hace falta |
| --- | --- | --- |
| **IP directa** | `ip:puerto` (por defecto `7777`, UDP) | Nada más. En la misma red local funciona tal cual; por internet el anfitrión abre el puerto UDP 7777 en su router |
| **Código (internet)** | Un código de 6 caracteres | Unity Relay (ver abajo) |

Al crear o entrar en una sala todos aparecen en el **lobby**, una plaza junto al camión donde se puede caminar mientras se van
uniendo los demás. Se ve el código o la IP, quién está listo, y el anfitrión elige **mapa y modo** y pulsa *Empezar*: todos son
teletransportados al mapa elegido, con la misma semilla (mismas cajas en los mismos sitios). Pulsa **TAB** en el lobby para usar el ratón.
La sala demo sin red sigue disponible escribiendo el código `AMETSA`.

### Activar el modo "Código" (Unity Relay)

1. En Unity: *Window > Package Manager > + > Add package by name*: `com.unity.services.relay` y `com.unity.services.authentication`.
2. *Project Settings > Services*: enlaza el proyecto con tu cuenta de Unity y activa Relay.
3. Listo: el assembly `Scripts/Net/Relay` solo se compila cuando existe el paquete de Relay y registra el servicio por sí solo.

### Qué se comparte en una partida online

Todos construyen el mismo nivel (mismo mapa, modo y semilla) y el anfitrión hace de árbitro cuando dos jugadores pueden hacer
lo mismo a la vez. Está sincronizado:

* **Jugadores**: posición, animación, nombre y colores, cajas en las manos, muerte (con su causa) y espectar. Los demás se ven como
  "fantasmas" sólidos: se les puede empujar (G), abrazar (H, cura a los dos), pasar cajas (E) o quitárselas (F), y ese efecto ocurre
  en la máquina del jugador real.
* **Cajas del mapa**: la que coge un jugador desaparece para todos; si dos la cogen a la vez gana el primero que llega al anfitrión
  y el otro la devuelve. Las que suelta un jugador al morir aparecen para todos.
* **Camión y pedido**: las entregas las valida el anfitrión, así que todos los camiones tienen las mismas cajas y el puzle se abre
  a la vez en todas las pantallas.
* **Puzle del camión**: el orden de las cajas, la salida del viaje y el final (explosión, mezcla mortal o éxito) los decide el anfitrión
  y se ven igual en todos.
* **Mundo**: el tronco giratorio, la balsa, las ráfagas de viento y los objetos que flotan siguen un reloj compartido. Al empezar,
  nadie se mueve hasta que todos han cargado el nivel. La pausa no detiene el mundo (los demás siguen jugando).

Sigue siendo local en cada máquina: dinero y manual (cada jugador cobra la entrega), el muñeco de nieve y los patos de goma.
Las cajas heladas no dejan rastro de hielo visible para los demás.

### Probar la red sin salir de Unity

* Tests de edición (`OnlineTests`, `OnlineWorldTests`): lógica del puzle en red, fantasmas, formato de direcciones.
* Partida real entre **dos procesos de Unity** (`OnlineWorldE2ETests`, se salta sola si no se le pide): abre dos copias del proyecto,
  elige una carpeta vacía `E2E` y lanza en cada una el test con `MIXEDUP_E2E_DIR=<carpeta>` y `MIXEDUP_E2E_ROLE=host` / `client`.
  Comprueba que ambos construyen las mismas cajas, que lo que coge uno desaparece en el otro, que una caja disputada acaba con un
  solo jugador, que las entregas llegan a los dos camiones y que el puzle acaba igual en ambos.

## Logros y pistas

El menú tiene un botón **Logros** (17 retos: saltar el tronco 67 veces, acariciar todos los patos, probar todas las combinaciones,
derribar el muñeco de nieve...). Cerca de las cajas difíciles aparece una pista (por ejemplo, agacharse en el túnel).

## Estructura (`MixedUp/Assets/_Project`)

| Carpeta | Qué hay |
| --- | --- |
| `Scripts/Core` | Estado del juego (`GameManager`), entradas (`GameInput`), idioma (`Localization`), preferencias (`GameSettings`) |
| `Scripts/Boxes` | `BoxData`, `BoxEffect` y los cuatro efectos (calor, eléctrico, hielo, tóxico). Todo son ScriptableObjects |
| `Scripts/Player` | Movimiento, inventario, estado, apariencia (`CharacterCustomization`), cajas en las manos (`CarriedBoxesView`) |
| `Scripts/World` | Cajas del mapa, camión, zonas peligrosas (agua, hielo, barro, fuego), tronco giratorio, setas saltarinas |
| `Scripts/Audio` | Todo el sonido sintetizado por código (`ProceduralAudio`): efectos, música y ambiente. No hay archivos de audio |
| `Scripts/Net` | Salas offline (`IRoomService`, `LocalRoomService`), y en `Online/` la red real (`OnlineSession`, `NetAvatar`, `OnlineLobby`); `Relay/` es opcional |
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
python Tools/generate_face_atlas.py      # las 16 expresiones de la cara
python Tools/generate_ui_art.py          # papel, carteles, sliders... y recortes de tus dibujos del Mixed_Up antiguo
```

Tras regenerarlo, vuelve a ejecutar **MixedUp > Build Prototype Scene**.

## Fuentes y licencias

* Fuente de la interfaz: **Bangers** (Vernon Adams), licencia SIL OFL 1.1.
* El repositorio es público: no se suben packs de terceros, solo arte propio.
