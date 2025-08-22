
# SmartRouteOptimizer

Optimización paralela de rutas de **última milla (Last-Mile)** con **C#/.NET 8**, **TPL** y **API REST**.
Ejecuta múltiples heurísticas (Greedy / Genético, etc.) **en paralelo**, comparte estado de manera **thread-safe**, reporta **métricas en tiempo real** y devuelve rutas para visualización (p. ej. con **Leaflet**).

> ✨ Ideal para escenarios tipo Amazon, UberEats, FedEx: múltiples pedidos dispersos, ventanas de tiempo, capacidad de vehículos, congestión y rutas variantes.

---

## Características

* ⚡ **Ejecución en paralelo** con Task Parallel Library (TPL): varias heurísticas en simultáneo.
* 🔁 **Orquestador de tareas** con `Task.WhenAll()` y cancelación por tiempo (`CancellationToken`).
* 🔒 **Estado compartido thread-safe** (locks) para progreso, evaluaciones y resultados.
* 📊 **Métricas en tiempo real**: evaluaciones, progreso %, tiempo, costo por algoritmo, costo por entrega.
* 🌍 **Rutas listas para el mapa** (coordenadas) y colores por vehículo.
* 🌱 **Escalable**: añade más tareas/heurísticas o divide por zonas geográficas.

---

## Arquitectura

* **Frontend Layer**: UI HTML5, mapa con Leaflet, monitores de progreso y resultados.
* **API Layer**: `OptimizerController` con endpoints `POST /start`, `GET /status/{id}`, `GET /result/{id}`.
* **Service Layer**: `OptimizationEngine` (motor), `Session Manager` y sesiones en memoria (`ConcurrentDictionary`).
* **Parallel Execution Layer**: orquestación con **Tasks** para Greedy/Genético + tarea de progreso.
* **Synchronization Layer**: `lock` para listas compartidas, `CancellationToken` y `Task.WhenAll()`.

---

## Diagrama (Mermaid)

```mermaid
graph TB
    subgraph "Frontend Layer"
        UI[HTML5 Interface]
        Map[Leaflet Map Component]
        Progress[Progress Monitor]
        Results[Results Display]
    end

    subgraph "API Layer"
        Controller[OptimizerController]
        StartAPI[POST /start]
        StatusAPI[GET /status/{id}]
        ResultAPI[GET /result/{id}]
    end

    subgraph "Service Layer"
        Engine[OptimizationEngine]
        SessionMgr[Session Manager]
        Sessions[(ConcurrentDictionary Sessions)]
    end

    subgraph "Parallel Execution Layer"
        Orchestrator[Task Orchestrator]
        
        subgraph "Algorithm Tasks"
            T1[Greedy-1 Task]
            T2[Greedy-2 Task]  
            T3[Genetic-1 Task]
            T4[Genetic-2 Task]
        end
        
        ProgressTask[Progress Update Task]
        SharedState[OptimizationSession<br/>Shared State]
    end

    subgraph "Synchronization Layer"
        Locks[Thread Locks]
        CancelTokens[Cancellation Tokens]
        TaskCoordination[Task.WhenAll()]
    end

    UI --> StartAPI
    Progress --> StatusAPI
    Results --> ResultAPI

    StartAPI --> Engine
    StatusAPI --> SessionMgr
    ResultAPI --> SessionMgr

    Engine --> Orchestrator
    Engine --> Sessions
    SessionMgr --> Sessions

    Orchestrator --> T1
    Orchestrator --> T2
    Orchestrator --> T3
    Orchestrator --> T4
    Orchestrator --> ProgressTask

    T1 --> SharedState
    T2 --> SharedState
    T3 --> SharedState
    T4 --> SharedState
    ProgressTask --> SharedState

    SharedState --> Locks
    T1 --> CancelTokens
    T2 --> CancelTokens
    T3 --> CancelTokens
    T4 --> CancelTokens
    ProgressTask --> CancelTokens

    Orchestrator --> TaskCoordination
```

---

## Estructura del proyecto

> Sugerencia de layout (ajústalo a tu repo actual):

```
SmartRouteOptimizer/
├── src/
│   └── SmartRouteOptimizer.Api/
│       ├── SmartRouteOptimizer.Api.csproj
│       ├── Program.cs
│       ├── Controllers/
│       │   └── OptimizerController.cs
│       ├── Models/
│       │   ├── OptimizationRequest.cs
│       │   ├── ClientDto.cs
│       │   ├── VehicleDto.cs
│       │   ├── AlgorithmStatusDto.cs
│       │   ├── ProgressDto.cs
│       │   ├── AlgorithmResultDto.cs
│       │   ├── RouteDto.cs
│       │   └── SolutionDto.cs
│       └── Services/
│           ├── OptimizationSession.cs
│           └── OptimizationEngine.cs
└── README.md
```

---

## Levantamiento rápido

```bash
# 1) Entrar al proyecto API
cd src/SmartRouteOptimizer.Api

# 2) Restaurar y ejecutar (requiere .NET 8)
dotnet restore
dotnet run

# 3) Swagger (dev)
# http(s)://localhost:5xxx/swagger
```

**CORS** está habilitado para pruebas locales. Restringe orígenes en producción.

---

## API Reference

### POST `/api/optimizer/start`

Inicia una optimización y devuelve `sessionId`.

**Body (JSON)**

```json
{
  "clients": [
    { "id": 1, "lat": 18.49, "lng": -69.93, "prioridad": 1, "ventanaInicio": 0, "ventanaFin": 3600, "nombre": "Cliente A" }
  ],
  "vehicles": [
    { "id": 101, "capacidad": 25, "color": "#1f77b4" }
  ],
  "timeLimitSeconds": 20
}
```

**200 OK**

```json
{ "sessionId": "6e4f8d7a-..." }
```

---

### GET `/api/optimizer/status/{sessionId}`

Devuelve **progreso en tiempo real**.

**200 OK**

```json
{
  "sessionId": "6e4f8d7a-...",
  "evaluaciones": 12500,
  "elapsedSeconds": 5.4,
  "progressPercent": 62.3,
  "algorithms": [
    { "name": "Greedy-1", "state": "Running", "cost": null, "lastUpdate": "2025-08-22T19:10:34.123Z" }
  ],
  "message": "Explorando 62% del espacio de soluciones..."
}
```

---

### GET `/api/optimizer/result/{sessionId}`

Devuelve **resultado final**.

**200 OK**

```json
{
  "sessionId": "6e4f8d7a-...",
  "results": [
    { "algorithm": "Greedy-1", "cost": 96.2, "distanceKm": 145.1, "timeSeconds": 2.0 }
  ],
  "best": { "algorithm": "Greedy-1", "cost": 96.2, "distanceKm": 145.1, "timeSeconds": 2.0 },
  "bestEfficiencyPercent": 88.7,
  "costPerDelivery": 4.81,
  "routes": [
    {
      "vehicleId": 101,
      "vehicleColor": "#1f77b4",
      "coordinates": [[18.4861,-69.9312],[18.49,-69.93],[18.4861,-69.9312]],
      "deliveries": 5,
      "estimatedDistanceKm": 27.5
    }
  ]
}
```

**404**: aún no finaliza.

---

## Modelo de datos

* **OptimizationRequest**

  * `clients: ClientDto[]`, `vehicles: VehicleDto[]`, `timeLimitSeconds: int`
* **ClientDto**: `id`, `lat`, `lng`, `prioridad`, `ventanaInicio`, `ventanaFin`, `nombre`
* **VehicleDto**: `id`, `capacidad`, `color`
* **ProgressDto**

  * `evaluaciones`, `elapsedSeconds`, `progressPercent`, `algorithms[]`, `message`
* **AlgorithmStatusDto**: `name`, `state` (`Waiting|Running|Completed`), `cost?`, `lastUpdate`
* **AlgorithmResultDto**: `algorithm`, `cost`, `distanceKm`, `timeSeconds`
* **SolutionDto**

  * `results[]`, `best`, `bestEfficiencyPercent`, `costPerDelivery`, `routes[]`
* **RouteDto**: `vehicleId`, `vehicleColor`, `coordinates [[lat,lng]]`, `deliveries`, `estimatedDistanceKm`

---

## Métricas y monitoreo

* ⏱️ **Tiempo**: `elapsedSeconds`
* 🧮 **Trabajo computacional**: `evaluaciones`
* 📈 **Progreso relativo**: `progressPercent`
* 💸 **Costo** por algoritmo: `AlgorithmResultDto.cost`
* 🥇 **Eficiencia**: `bestEfficiencyPercent`
* 🧾 **Costo por entrega**: `costPerDelivery`
* 🔎 **Estados individuales**: `algorithms[]` (running/completed + lastUpdate)

> Estas métricas permiten comparar heurísticas, evaluar escalabilidad y analizar desempeño por sesión.

---

## Paralelización y sincronización

* **Estrategia principal**: paralelización **por heurística** (Greedy/Genético) + tarea de progreso.
* **Orquestación**: `Task.Run(...)` para cada algoritmo y `Task.WhenAll(...)` para sincronizar la finalización.
* **Límite de tiempo**: `CancellationTokenSource(TimeSpan.FromSeconds(...))`.
* **Estado compartido**: `OptimizationSession` con `lock` al modificar `Algorithms` (thread-safe).
* **Escalabilidad**: agrega más tasks/algoritmos o divide por zonas/candidatos.

Ejemplo (resumen):

```csharp
var tasks = new List<Task<AlgorithmResultDto>>
{
    Task.Run(() => RunGreedy("Greedy-1", 2.0, session, rng, token), token),
    Task.Run(() => RunGenetic("Genético-1", 7.0, session, rng, token), token),
    // ...
};
var results = await Task.WhenAll(tasks);
```

Actualización segura:

```csharp
lock (session.Algorithms)
{
    var i = session.Algorithms.FindIndex(a => a.Name == name);
    if (i >= 0) session.Algorithms[i] = session.Algorithms[i] with
    {
        State = state,
        Cost = cost,
        LastUpdate = DateTimeOffset.UtcNow
    };
}
```

---

## Integración con Frontend (Leaflet)

```js
// 1) Iniciar optimización
const start = await fetch('/api/optimizer/start', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ clients, vehicles, timeLimitSeconds: 20 })
});
const { sessionId } = await start.json();

// 2) Polling de progreso
const poll = setInterval(async () => {
  const r = await fetch(`/api/optimizer/status/${sessionId}`);
  if (!r.ok) return;
  const s = await r.json();
  // Actualiza UI: barra de progreso, evaluaciones, estados por algoritmo, etc.
}, 200);

// 3) Al finalizar ventana de tiempo, obtener resultado y dibujar rutas
setTimeout(async () => {
  clearInterval(poll);
  const rr = await fetch(`/api/optimizer/result/${sessionId}`);
  if (!rr.ok) return;
  const result = await rr.json();
  // Dibujar polylines con result.routes en Leaflet
}, 20000);
```

---

## Roadmap

* [ ] Sustituir simulaciones por heurísticas VRP/TSP reales (con ventanas de tiempo).
* [ ] División por **zonas geográficas** y balanceo de carga por vehículo.
* [ ] Cache/persistencia en **Redis** para sesiones distribuidas.
* [ ] Métricas avanzadas: penalizaciones por tardanza, SLAs, huella de carbono.
* [ ] UI completa con gráficos de rendimiento y comparación de heurísticas.

---

## Contribuir

1. Haz un fork y crea una rama: `feat/nueva-heuristica`
2. Asegúrate de mantener interfaces: `AlgorithmResultDto`, actualización de `Algorithms` y métricas.
3. Abre un PR con una breve explicación de la estrategia y benchmarks.

---

## Licencia

MIT (puedes cambiarla si tu proyecto lo requiere).

---

## Créditos

Proyecto académico/técnico de **optimización de última milla** con paralelización en .NET.
Nombre del proyecto: **SmartRouteOptimizer**.

---

¿Quieres que lo deje ya en un archivo `README.md` y además te genere un `api.http` con ejemplos de llamadas (`POST /start`, `GET /status`, `GET /result`) para probar desde VS Code/REST Client?
