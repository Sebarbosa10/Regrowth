# REGROWTH — guía del proyecto

Juego VR para **Meta Quest 2**. El jugador escucha un lore sobre un planeta contaminado y, dentro de una "simulación", destruye basura flotante con una pistola que tiene 4 modos de disparo. Cada modo solo destruye un tipo de basura.

- Unity **2022.3.10f1**, **URP 14.0.8**, Meta XR SDK **72.0.0** (`com.meta.xr.sdk.all`) + Oculus XR Plugin 4.0.0
- Android: Vulkan, IL2CPP, ARM64, Linear, **Multiview** (Single Pass Instanced), Quest 2
- Objetivo: **72 fps estables** (13,8 ms/frame)

---

## 1. Escenas y flujo

| Escena | Build index | Contenido |
|---|---|---|
| `Assets/Scenes/Menu.unity` | 0 | Menú principal: `MainMenu` + `FadeController`. Play → fade → `SceneManager.LoadScene("SampleScene")` |
| `Assets/Scenes/SampleScene.unity` | 1 | Todo el juego (3 rondas). Al terminar vuelve a `"Menu"` (valor serializado en `StageManager.mainMenuSceneName`) |

**Flujo de una partida (SampleScene):**
1. `NarrativeBeatManager.Start` reproduce el beat `GameStart` (audio + subtítulos).
2. `StageManager.InitWithDelay` (espera 2 frames) → armas a sus holsters → `LoadRound(0)` (diálogos de la ronda).
3. El jugador saca un arma del holster → `WeaponHolster.OnWeaponRemoved` → `StageManager` spawnea la basura de la ronda (15 / 30 / 60 objetos, en una caja alrededor de `boxCenter`). En la ronda 0 se dispara además `FirstGrabDissolveEvent` (aparece el `OCEAN` con dissolve).
4. Cada basura destruida → `TrashObject.OnDestroy` → `StageManager.OnTrashDestroyed` → HUD.
5. Basura a 0 + todas las armas guardadas → `TransitionToNextRound`: bloquear armas → beat de fin de ronda → fade out + transición de color del Volume (`PollutionVolumeController`) → destruir restos → siguiente ronda → fade in.
6. Tras la ronda 3 → `Menu`.

Mientras suena un beat narrativo (`NarrativeBeatManager.IsPlaying`) no se puede disparar ni sacar/guardar armas.

## 2. Jerarquía de SampleScene (raíces)

| Raíz | Notas |
|---|---|
| `[BuildingBlock] Camera Rig` | Rig de Meta Building Blocks (OVRCameraRig + OVRManager + Interaction SDK: manos, controladores, grab). ~577 GameObjects, 40 SkinnedMeshRenderers (modelos de controlador/manos) |
| `Stage 1` | **El escenario/props**: 117 GameObjects, **115 MeshRenderers**, todos instancias de `Graphics/SeaPlants/SeaPlants.fbx` (16 sub-mallas distintas). Contiene `OCEAN` |
| `PlasmaGun` | Arma principal (`TrashGun`, `GunEnergySystem`, `GunModeColorizer`, `GunRecoil`, `TwoHandedGunGrip`), 22 renderers con sombra |
| `LeftThighHolster`, `SIM BOT` | Holster y otro objeto |
| `StageManager`, `NarrativeBeatManager`, `DissolveEvent`, `updatemanager`, `audiomanager`, `Music`, `UnderwaterSound`, `Audio Reverb Zone`, `EventSystem` | Managers / audio |

Un `Volume` local (no global) con `Scenes/SampleScene/Box Volume Profile.asset`: Bloom, ShadowsMidtonesHighlights y Vignette activos.

## 3. XR Rig, input, interacción y locomoción

- **Rig**: Meta Building Blocks (`OVRCameraRig`), tracking origin = Floor. OVRManager con `useRecommendedMSAALevel` y dynamic resolution activada (min scale 1 → en la práctica no baja).
- **Interacción**: Meta Interaction SDK (`Oculus.Interaction.Grabbable`, `GrabInteractable`, `HandGrabInteractable`). Los scripts consultan `Grabbable.SelectingPointsCount > 0` para saber si el arma está agarrada.
- **Input**: `OVRInput` directo (no Input System ni XRI).
  - Gatillo derecho (`PrimaryIndexTrigger`, RTouch) → disparar (umbral 0,7).
  - Botón A (`Button.One`) → cambiar modo de disparo.
  - Botón X (`Button.Three`) → mostrar/ocultar menú de controles.
  - Agitar el arma cuando no tiene energía → recarga.
  - Agarre de dos manos: `TwoHandedGunGrip` compara la posición del mando (en tracking space) con dos anclas del arma.
- **Locomoción**: no hay; el jugador está quieto (room-scale).
- **Haptics**: `OVRInput.SetControllerVibration` + corrutina para pararla.

## 4. Mapa de carpetas (solo lo propio)

```
Assets/
  Scripts/            Todo el código del juego (sin namespaces ni asmdef)
    UI/               RoundHUDDisplay
  Scenes/             Menu, SampleScene (+ Box Volume Profile)
  Graphics/
    Materials/        Materiales de modos del arma (Emissive*, Metal, red)
    Prefabs/          Bullet*, Lasersight, TraceBullet (LineRenderer del hitscan)
      TRASH/          GLASS, METAL, ORGANIC, PLASTIC (prefabs de basura + modelos/texturas)
    SeaPlants/        Modelo y materiales de las plantas marinas (los "props")
    Shaders/          Dissolve.ShaderGraph (+ Dissolve.mat)
    Terrain/, Vacuum/, Pistol/
    *.mp3             Algunos SFX sueltos aquí (deberían estar en Audio/)
  Audio/              Música, beats narrativos, SFX
  Settings/           Assets URP (Performant / Balanced / HighFidelity) + renderers
  Resources/          Configs de Meta XR / OVR
  XR/, Oculus/, Plugins/Android/   Configuración XR y AndroidManifest
Terceros (no tocar): Houidisoft technology/ (Plasma Shader), IgniteCoders/ (Simple Water Shader), TextMesh Pro/, TutorialInfo/
```

## 5. Scripts

**Núcleo / managers**
| Script | Qué hace |
|---|---|
| `CustomUpdateManager` | Singleton que llama `Tick(dt)` a los `IUpdatable` registrados (un solo `Update` para todos) |
| `IUpdatable` | Interfaz `Tick(float deltaTime)` |
| `StageManager` | Singleton. Rondas (`RoundConfig[]`), spawn de basura en caja, escucha eventos de holsters, transición entre rondas, vuelta al menú |
| `NarrativeBeatManager` | Singleton. 6 beats (audio + subtítulos) por inicio/fin de ronda; expone `IsPlaying` que bloquea el gameplay |
| `DialogueManager` | Singleton. Cola de clips de diálogo por ronda |
| `SubtitleDisplay` | Singleton. Efecto máquina de escribir en un TMP |
| `FadeController` | Fade a negro con una `Image` (corrutinas `FadeOut`/`FadeIn`) |
| `PollutionVolumeController` | Interpola ShadowsMidtonesHighlights del Volume por ronda |
| `FirstGrabDissolveEvent` | Alterna dissolve (MaterialPropertyBlock `_Dissolve` 0↔2) sobre `appearRoots`/`disappearRoots` + fade de audio. Los renderers se cachean en `Awake` y se apagan mientras están disueltos. `toggleRoots` = raíces con material opaco (sin dissolve) que se activan/desactivan con `SetActive` a mitad del efecto. Solo el grupo pequeño de plantas con el shader Dissolve debe ir en `appearRoots` |
| `MainMenu` | Botones Play/Quit del menú |

**Arma**
| Script | Qué hace |
|---|---|
| `TrashGun` | Hitscan con 4 modos (Single→Plastic, Burst→Glass, Auto→Organic, Spread→Metal). Destruye si el tag coincide. Pool de trazas `LineRenderer`, haptics, sonido |
| `GunEnergySystem` | 10 disparos; al agotarse se recarga agitando el mando 3 s |
| `GunModeColorizer` | Cambia el material del arma según modo / energía |
| `GunRecoil` | Retroceso visual del modelo (Update propio) |
| `TwoHandedGunGrip` | Detecta mano principal / segunda mano y rota el Rigidbody (FixedUpdate) |
| `WeaponHolster` | Guarda/saca el arma, auto-retorno si se aleja > 2 m, eventos `OnWeaponStored/Removed`, `Lock/Unlock` |
| `WeaponMarker` | Marca qué holster pertenece a un arma |

**Basura**
| Script | Qué hace |
|---|---|
| `TrashObject` | Avisa a `StageManager` en `OnDestroy` |
| `TrashFloat` | Flotación/deriva/rotación, parámetros por ronda (IUpdatable) |
| `TrashDissolveSpawner` | Dissolve de aparición y colliders desactivados mientras tanto |
| `TrashBullet` | Proyectil físico por tag. **Parece legado**: `TrashGun` usa hitscan |

**UI / debug**: `RoundHUDDisplay` (singleton, total/destruidas), `FPSDisplay`, `ControlsMenuToggle`, `NoiseScaleAnimator` (anima `_Noise_Scale` con MPB).

## 6. Convenciones detectadas (seguirlas)

- Sin namespaces, sin asmdef. Un `MonoBehaviour` por archivo, nombre de clase = archivo, PascalCase.
- Campos privados `camelCase` con `[SerializeField] private`. Algún campo privado con `_camelCase` (`TwoHandedGunGrip`), evitar mezclar: usar `camelCase`.
- Propiedades de solo lectura con expression body: `public bool IsDepleted => isDepleted;`.
- Singletons: `public static X Instance { get; private set; }` + guard en `Awake` (algunos usan campo público sin guard: `StageManager`, `DialogueManager`, `RoundHUDDisplay`).
- Comunicación: singletons + `event Action` (holsters). Nada de ScriptableObjects para datos: la configuración son clases `[System.Serializable]` anidadas (`RoundConfig`, `HapticProfile`, `RecoilProfile`, `FloatConfig`…) en arrays en el Inspector.
- Lógica por frame: preferir `IUpdatable` + `CustomUpdateManager` en vez de `Update()`.
- Secuencias temporales con corrutinas.
- Propiedades de shader cacheadas con `Shader.PropertyToID` y aplicadas con `MaterialPropertyBlock`.
- `Debug.Log` con prefijo `[NombreClase]`. Comentarios y logs en español. `[Header]`/`[Tooltip]` en español.
- Separadores de secciones con comentarios en bloque (`// ─── PUBLIC ───`; en el repo aparecen como `?????` por problemas de encoding).
- Null-checks defensivos (`?.`, `if (x == null) return;`).

---

## 7. Deuda técnica y riesgos

### 7.1 Rendimiento — el problema de los props (prioridad máxima)

Con `Stage 1` presente el juego va a ~30 fps; sin él va bien. Estas son las causas probables, ordenadas por impacto estimado:

1. **Las 115 plantas usan `Dissolve.ShaderGraph`** (`SeaPlants_MAT` y `BigSeaPlants_MAT`), que es:
   - **URP Lit** completo (PBR, specular, environment reflections).
   - **Alpha Clip activado** → `clip()` en el fragment. En GPUs tile-based (Adreno 650) rompe el early-Z y es caro con MSAA.
   - **Render Face = Both** (doble cara) → más fragmentos y overdraw.
   - **Noise node + Dither node** por píxel.
   Un material de escenario estático necesita un shader Lit/Simple Lit (o Unlit) opaco, sin clip, de una cara.
2. **El dissolve por MaterialPropertyBlock rompe el SRP Batcher.** `FirstGrabDissolveEvent` escribe un MPB en todos los renderers bajo `OCEAN` (las plantas). Un renderer con MPB sale del SRP Batcher, y el MPB se queda puesto para siempre, así que cada planta es un draw call caro independiente (x pasadas).
3. **Antes del primer agarre las plantas están "invisibles" (`_Dissolve = 2`) pero se siguen renderizando**: vertex + fragment + clip de todos los píxeles, y además siguen proyectando sombra.
4. **Sombras**: las 115 plantas proyectan y reciben sombra (`Cast Shadows = On`). Android usa la calidad **Balanced** (`URP-Balanced.asset`): sombras de la main light 1024, soft shadows, distancia 50 m. El pase de sombras vuelve a dibujar todas las plantas.
5. **SSAO activo** en `URP-Balanced-Renderer` (el renderer que usa Android). En Quest es muy caro: pase extra de depth/normals (otra vez todas las plantas) + pase de pantalla completa, y fuerza textura intermedia.
6. **Post-procesado en la cámara del jugador** (`Render Post Processing = On`, Bloom + Vignette + SMH). En Quest obliga a una textura intermedia y a un resolve extra por ojo; solo el Bloom ya cuesta varios ms. Se podría sustituir el tinte de contaminación por fog/ambient color o por un overlay.
7. **Sin occlusion culling ni LODGroups**, y far clip plane de la cámara = 50 m, cuando la niebla exponencial² (densidad 0,13) ya hace invisible todo a partir de ~15 m. Todo lo que está entre ~15 y 50 m se dibuja y no se ve.
8. **GPU instancing desactivado** en los materiales de las plantas. Todas son el mismo FBX, así que instancing o static batching deberían ayudar si el shader lo permite (con MPB tampoco funciona).
9. Fixed Foveated Rendering: no se ve configurado en el `OVRManager`. Conviene activarlo (High) en Quest 2.

> Para confirmarlo antes de tocar nada: **OVR Metrics Tool** (GPU/CPU level, GPU %) y **RenderDoc / Frame Debugger** en el Quest. Si la GPU está al 100 % y la CPU no, es GPU-bound (lo esperado según lo anterior).

### 7.2 Otros problemas de rendimiento en código
- `GunModeColorizer.SetEnergyLerp` / `ApplyMaterial` usan `renderer.materials` → **instancia materiales nuevos y aloca un array en cada disparo** (fuga de materiales + saca el arma del batching). Usar `sharedMaterials` o un MPB.
- `SubtitleDisplay.TypeAndHide`: `Substring` + `new WaitForSeconds` **por carácter** → GC constante durante los subtítulos. Usar `maxVisibleCharacters` de TMP y cachear el `WaitForSeconds`.
- `TrashGun.Hitscan`: `Physics.Raycast` **sin LayerMask** (el campo `shootableLayer` existe pero no se usa) → el raycast choca con todo, plantas incluidas si tienen collider.
- `AudioSource.PlayClipAtPoint` en cada impacto/dissolve crea y destruye un GameObject → usar un pool de AudioSources.
- La basura se instancia y destruye sin pool (15/30/60 `Instantiate` en un solo frame al empezar la ronda → pico).
- `new WaitForSeconds(...)` y `new WaitUntil(...)` en corrutinas recurrentes (burst, vibración, beats).
- `GunRecoil.Update` y `ControlsMenuToggle.Update` corren siempre, fuera del `CustomUpdateManager`.
- `TwoHandedGunGrip` usa `FixedUpdate` con Fixed Timestep 0,02 (50 Hz) y la pantalla va a 72 Hz → jitter en la rotación a dos manos.
- Physics collision matrix: todas las capas colisionan con todas.
- `StageManager.SetWeaponInteractable` usa `GetComponentsInChildren` (solo en transiciones, aceptable).

### 7.3 Bugs / riesgos lógicos
- `TrashObject.OnDestroy` también se ejecuta al limpiar la ronda (`DestroyActiveTrash`) y al descargar la escena → el contador de basura se descuenta de más (inofensivo hoy porque la ronda ya está limpia, pero frágil).
- `TrashFloat` usa `CustomUpdateManager.Instance` en `OnEnable`, mientras que el resto usa una referencia serializada. Si `CustomUpdateManager.Awake` aún no se ha ejecutado, el objeto no se registra nunca.
- `GunEnergySystem` vibra siempre el `RTouch`, aunque el arma esté en la mano izquierda. `TryShoot` consume el último disparo y devuelve `false` (el disparo 10 no sale).
- `StageManager.Instance` y `DialogueManager.Instance` sin guard de duplicados; `Instance` estáticos no se limpian en `OnDestroy`.
- Scripts no usados o legado: `TrashBullet`, prefabs `Bullet*`, `Graphics/Vacuum` (la "vacuum gun" referenciada en `StageManager`).
- Encoding: varios `.cs` tienen caracteres rotos (`�`, `?????`). Guardar en UTF-8.
- `TempAssembly.dll` y `RuntimeActionBindings.json` en la raíz del repo; texturas y audio duplicados (`Terrain/*.jpg` y `*.png`).
- El git status muestra `.tif`/`.png` de terceros borrados tras la migración a LFS: comprobar que no falten en el proyecto.

---

## 8. Reglas de trabajo con Claude

- Antes de editar, decir qué archivos se tocan y por qué.
- **No editar `.unity`, `.prefab` ni `.meta` a mano** salvo que se pida; los cambios de editor se describen paso a paso.
- **Nunca borrar ni renombrar `.meta`.**
- Prioridad: 72 fps estables en Quest 2. Nada de allocations por frame, `Find`/`GetComponent` en runtime ni `Update` sueltos (usar `IUpdatable`).
- Seguir las convenciones de la sección 6 y actualizar este archivo cuando cambie la arquitectura.
- Si algo es ambiguo, preguntar.

## 9. Decisiones tomadas (2026-10-01)

- Los ~30 fps están medidos **en el Quest 2** con `FPSDisplay` (sin datos de OVR Metrics Tool).
- **Dissolve**: basta con un grupo pequeño de plantas donde se note; el resto puede usar un material opaco simple.
- **Sombras de las plantas: no hacen falta.** Se pueden desactivar.
- **Post-procesado: se mantiene** (es importante para la estética). Solo se puede abaratar, no quitar.
- Objetivo: **72 fps estables** es suficiente.

## 10. Preguntas abiertas

1. ¿Qué es exactamente `OCEAN` y qué hay dentro de `Stage 1` aparte de las plantas? ¿Existe un suelo/terreno (`Terrain.fbx`, `New Terrain 1.asset`) en la escena o está sin usar?
2. ¿Hay alguna luz direccional en tiempo real? No encontré componentes `Light` en la escena (quizás vienen de un prefab). ¿Alguna otra sombra importa (arma, basura)?
3. ¿La "vacuum gun" y `TrashBullet` siguen en uso o se pueden considerar legado?
4. ¿Se trabaja con otra persona en la escena (para coordinar cambios en `SampleScene.unity`)?
