const API_BASE = "http://localhost:5120/api/optimizer";
const SANTO_DOMINGO_CENTER = [18.4861, -69.9312];

// ===== MAPA =====
let map = L.map('map').setView(SANTO_DOMINGO_CENTER, 12);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: 'Leaflet | © OpenStreetMap'
}).addTo(map);

// Marker del depósito central
L.marker(SANTO_DOMINGO_CENTER, {
    icon: L.icon({
        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-red.png',
        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
    })
}).addTo(map).bindPopup("🏭 Depósito Central");

// ===== VARIABLES GLOBALES =====
let sessionId = null;
let intervalStatus = null;
let currentClients = [];
let currentVehicles = [];
let routeLayerGroup = L.layerGroup().addTo(map);
let legendControl = null; // Control de leyenda

// ===== SLIDERS =====
document.getElementById("clientes").oninput = e => document.getElementById("clientesVal").innerText = e.target.value;
document.getElementById("vehiculos").oninput = e => document.getElementById("vehiculosVal").innerText = e.target.value;
document.getElementById("tiempo").oninput = e => document.getElementById("tiempoVal").innerText = e.target.value;

// ===== GENERAR DATOS SIMULADOS =====
function generarClientes(cantidad) {
    const clientes = [];
    for (let i = 1; i <= cantidad; i++) {
        // Generar coordenadas alrededor de Santo Domingo
        const lat = SANTO_DOMINGO_CENTER[0] + (Math.random() - 0.5) * 0.05;
        const lng = SANTO_DOMINGO_CENTER[1] + (Math.random() - 0.5) * 0.05;

        clientes.push({
            id: i,
            lat: lat,
            lng: lng,
            prioridad: Math.floor(Math.random() * 3) + 1, // 1-3
            ventanaInicio: 8.0 + Math.random() * 2, // 8-10 AM
            ventanaFin: 16.0 + Math.random() * 4, // 4-8 PM
            nombre: `Cliente ${i}`
        });
    }
    return clientes;
}

function generarVehiculos(cantidad) {
    const colores = ["#ff0000", "#00ff00", "#0000ff", "#ffff00", "#ff00ff", "#00ffff", "#ffa500", "#800080"];
    const vehiculos = [];

    for (let i = 1; i <= cantidad; i++) {
        vehiculos.push({
            id: i,
            capacidad: Math.floor(80 + Math.random() * 40),
            color: colores[(i - 1) % colores.length]
        });
    }
    return vehiculos;
}

// ===== LEYENDA DEL MAPA =====
function crearLeyenda(rutas, mejorAlgoritmo = null) {
    if (legendControl) {
        map.removeControl(legendControl);
    }

    legendControl = L.control({ position: 'topright' });

    legendControl.onAdd = function (map) {
        const div = L.DomUtil.create('div', 'legend-control');

        div.innerHTML = `
            <div style="
                background: rgba(255, 255, 255, 0.95); 
                border: 2px solid rgba(0,0,0,0.2); 
                border-radius: 8px; 
                padding: 12px; 
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                min-width: 200px;
                max-width: 250px;
                max-height: 400px; /* Limite de altura */
                overflow-y: auto; /* Scroll vertical */
            ">
                <div style="
                    font-weight: bold; 
                    margin-bottom: 8px; 
                    color: #333;
                    font-size: 14px;
                    border-bottom: 1px solid #ddd;
                    padding-bottom: 6px;
                ">
                    📍 Leyenda de Rutas
                </div>
                
                ${mejorAlgoritmo ? `
                    <div style="
                        background: linear-gradient(45deg, #28a745, #20c997);
                        color: white;
                        padding: 6px 8px;
                        border-radius: 4px;
                        font-size: 11px;
                        font-weight: bold;
                        margin-bottom: 8px;
                        text-align: center;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    ">
                        🏆 Mejor: ${mejorAlgoritmo}
                    </div>
                ` : ''}

                <!-- Centro de Distribución -->
                <div style="display: flex; align-items: center; margin-bottom: 6px;">
                    <div style="
                        width: 12px; 
                        height: 12px; 
                        background: #dc3545; 
                        border-radius: 50%; 
                        margin-right: 8px;
                        border: 2px solid white;
                        box-shadow: 0 1px 3px rgba(0,0,0,0.3);
                    "></div>
                    <span style="font-size: 12px; color: #555;">Centro Distribución</span>
                </div>

                <!-- Clientes -->
                <div style="display: flex; align-items: center; margin-bottom: 8px;">
                    <div style="
                        width: 12px; 
                        height: 12px; 
                        background: #28a745;  
                        border-radius: 50%; 
                        margin-right: 8px;
                        border: 2px solid white;
                        box-shadow: 0 1px 3px rgba(0,0,0,0.3);
                    "></div>
                    <span style="font-size: 12px; color: #555;">Cliente</span>
                </div>

                <!-- Rutas con scroll -->
                <div style="max-height: 250px; overflow-y: auto;">
                    ${rutas.map(ruta => `
                        <div style="margin-bottom: 6px; padding: 4px; background: rgba(248,249,250,0.8); border-radius: 4px;">
                            <div style="display: flex; align-items: center; margin-bottom: 2px;">
                                <div style="
                                    width: 20px; 
                                    height: 3px; 
                                    background: ${ruta.vehicleColor}; 
                                    margin-right: 8px;
                                    border-radius: 2px;
                                    box-shadow: 0 1px 2px rgba(0,0,0,0.2);
                                "></div>
                                <span style="
                                    font-size: 11px; 
                                    color: #444;
                                    font-weight: 600;
                                ">Vehículo ${ruta.vehicleId}</span>
                            </div>
                            <div style="
                                font-size: 10px; 
                                color: #666;
                                margin-left: 28px;
                                line-height: 1.2;
                            ">
                                📦 ${ruta.deliveries} entregas<br>
                                ${ruta.algorithm ? `🤖 ${ruta.algorithm}` : ''}
                            </div>
                        </div>
                    `).join('')}
                </div>
            </div>
        `;

        L.DomEvent.disableClickPropagation(div);
        return div;
    };

    legendControl.addTo(map);
}

// ===== FUNCIÓN CORREGIDA CON DEBUGGING =====
async function iniciarOptimizacion() {
    try {
        // Limpiar resultados previos
        document.getElementById("resultadosCard").style.display = "none";
        routeLayerGroup.clearLayers();

        // Remover leyenda anterior
        if (legendControl) {
            map.removeControl(legendControl);
            legendControl = null;
        }

        // Generar datos
        const numClientes = parseInt(document.getElementById("clientes").value);
        const numVehiculos = parseInt(document.getElementById("vehiculos").value);
        const tiempoLimite = parseInt(document.getElementById("tiempo").value);

        currentClients = generarClientes(numClientes);
        currentVehicles = generarVehiculos(numVehiculos);

        // Mostrar clientes en el mapa
        mostrarClientesEnMapa();

        // VALIDACIONES ANTES DE ENVIAR
        console.log("🔍 Validando datos antes de enviar:");
        console.log("- Número de clientes:", currentClients?.length || 0);
        console.log("- Número de vehículos:", currentVehicles?.length || 0);
        console.log("- Tiempo límite:", tiempoLimite);

        // Validar que no estén vacíos
        if (!currentClients || currentClients.length === 0) {
            throw new Error("No hay clientes generados");
        }
        if (!currentVehicles || currentVehicles.length === 0) {
            throw new Error("No hay vehículos generados");
        }

        // VALIDAR TIPOS
        const request = {
            clients: currentClients.map(c => ({
                id: parseInt(c.id),
                lat: parseFloat(c.lat),
                lng: parseFloat(c.lng),
                prioridad: parseInt(c.prioridad),
                ventanaInicio: parseFloat(c.ventanaInicio),
                ventanaFin: parseFloat(c.ventanaFin),
                nombre: c.nombre?.toString() || ""
            })),
            vehicles: currentVehicles.map(v => ({
                id: parseInt(v.id),
                capacidad: parseInt(v.capacidad),
                color: v.color?.toString() || "#000000"
            })),
            timeLimitSeconds: parseInt(tiempoLimite)
        };

        console.log("📤 Request final:", JSON.stringify(request, null, 2));
        console.log("🔗 Probando conexión a:", `${API_BASE}/start`);

        const response = await fetch(`${API_BASE}/start`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Accept": "application/json"
            },
            body: JSON.stringify(request)
        });

        console.log("📡 Response status:", response.status);
        console.log("📡 Response headers:", [...response.headers.entries()]);

        const responseText = await response.text();
        console.log("📡 Response body:", responseText);

        if (!response.ok) {
            let errorMessage = `Error ${response.status}`;
            try {
                const errorData = JSON.parse(responseText);
                errorMessage += `: ${errorData.message || errorData.title || JSON.stringify(errorData)}`;
            } catch {
                errorMessage += `: ${responseText}`;
            }
            throw new Error(errorMessage);
        }

        // Parsear la respuesta exitosa
        const result = JSON.parse(responseText);
        sessionId = result.sessionId;

        console.log("✅ SessionId obtenido:", sessionId);

        // Iniciar polling
        if (intervalStatus) clearInterval(intervalStatus);
        intervalStatus = setInterval(obtenerStatus, 500);

    } catch (error) {
        console.error("❌ Error completo:", error);
        alert("Error al iniciar optimización: " + error.message);
    }
}

async function testearConexionAPI() {
    try {
        console.log("🧪 Testeando conexión API...");

        // Test básico de conectividad
        const response = await fetch(`${API_BASE.replace('/api/optimizer', '')}/swagger/index.html`, {
            method: "GET"
        });

        console.log("🧪 Test conectividad - Status:", response.status);

        if (response.status === 200) {
            console.log("✅ API está funcionando - Swagger accesible");
        } else if (response.status === 404) {
            console.log("⚠️ Swagger no encontrado, pero API podría estar funcionando");
        } else {
            console.log("❌ Posible problema de conectividad");
        }

    } catch (error) {
        console.error("❌ Error de conectividad:", error.message);
        alert("No se puede conectar a la API. Verifica que esté corriendo en " + API_BASE);
    }
}

function debugRequest() {
    console.log("🔧 DEBUGGING COMPLETO:");
    console.log("API_BASE:", API_BASE);
    console.log("currentClients:", currentClients);
    console.log("currentVehicles:", currentVehicles);

    if (currentClients.length > 0) {
        console.log("Estructura primer cliente:", currentClients[0]);
        console.log("Tipos:", {
            id: typeof currentClients[0].id,
            lat: typeof currentClients[0].lat,
            lng: typeof currentClients[0].lng,
            prioridad: typeof currentClients[0].prioridad,
            ventanaInicio: typeof currentClients[0].ventanaInicio,
            ventanaFin: typeof currentClients[0].ventanaFin,
            nombre: typeof currentClients[0].nombre
        });
    }

    if (currentVehicles.length > 0) {
        console.log("Estructura primer vehículo:", currentVehicles[0]);
        console.log("Tipos:", {
            id: typeof currentVehicles[0].id,
            capacidad: typeof currentVehicles[0].capacidad,
            color: typeof currentVehicles[0].color
        });
    }
}

async function obtenerStatus() {
    if (!sessionId) return;

    try {
        const response = await fetch(`${API_BASE}/status/${sessionId}`);
        if (!response.ok) return;

        const data = await response.json();

        // USAR NOMBRES CORRECTOS DE LA API
        document.getElementById("evaluaciones").innerText = data.evaluaciones.toLocaleString();
        document.getElementById("tiempoTranscurrido").innerText = data.elapsedSeconds.toFixed(1);
        document.getElementById("progressBar").style.width = data.progressPercent + '%';
        document.getElementById("progressText").innerText = data.message;

        // Estado de algoritmos
        let container = document.getElementById("algoritmosEstado");
        container.innerHTML = "";

        data.algorithms.forEach(alg => {
            let div = document.createElement("div");
            div.className = `algorithm-item algorithm-${alg.state.toLowerCase()}`;

            let estado = alg.state;
            let costo = alg.cost ? ` (Costo: ${alg.cost.toFixed(2)})` : '';

            div.innerHTML = `<strong>${alg.name}</strong>: ${estado}${costo}`;
            container.appendChild(div);
        });

        if (data.progressPercent >= 100) {
            clearInterval(intervalStatus);
            intervalStatus = null;

            // Esperar un poco antes de obtener resultados
            setTimeout(obtenerResultado, 1000);
        }

    } catch (error) {
        console.error("❌ Error al obtener status:", error);
    }
}

async function obtenerResultado() {
    if (!sessionId) return;

    try {
        const response = await fetch(`${API_BASE}/result/${sessionId}`);
        if (!response.ok) {
            console.log("⏳ Resultado aún no disponible, reintentando...");
            setTimeout(obtenerResultado, 2000);
            return;
        }

        const data = await response.json();
        console.log("📦 Solución:", data);

        mostrarResultados(data);

        if (data.routes && data.routes.length > 0) {
            // Pasar información del mejor algoritmo a la función de dibujo
            const mejorAlgoritmo = data.best?.algorithm || null;
            dibujarRutas(data.routes, mejorAlgoritmo);
        }

    } catch (error) {
        console.error("❌ Error al obtener resultado:", error);
    }
}

function mostrarClientesEnMapa() {
    // Limpiar markers previos de clientes
    routeLayerGroup.clearLayers();

    currentClients.forEach(cliente => {
        L.marker([cliente.lat, cliente.lng], {
            icon: L.icon({
                iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-blue.png',
                shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
                iconSize: [20, 32],
                iconAnchor: [10, 32],
            })
        }).addTo(routeLayerGroup)
            .bindPopup(`${cliente.nombre}<br>Prioridad: ${cliente.prioridad}`);
    });
}

// ===== FUNCIÓN MEJORADA PARA DIBUJAR RUTAS CON LEYENDA =====
function dibujarRutas(rutas, mejorAlgoritmo = null) {
    routeLayerGroup.clearLayers();

    // Enriquecer rutas con información del algoritmo si está disponible
    const rutasConAlgoritmo = rutas.map(ruta => ({
        ...ruta,
        algorithm: mejorAlgoritmo // Por ahora asignamos el mejor algoritmo a todas las rutas
    }));

    rutasConAlgoritmo.forEach((ruta, index) => {
        const puntos = ruta.coordinates;

        // Dibujar la polyline con mejor estilo
        const polyline = L.polyline(puntos, {
            color: ruta.vehicleColor,
            weight: 4,
            opacity: 0.8,
            lineCap: 'round',
            lineJoin: 'round'
        }).addTo(routeLayerGroup);

        // Popup mejorado con información del algoritmo
        polyline.bindPopup(`
            <div style="text-align: center; font-family: 'Segoe UI', Arial, sans-serif; min-width: 180px;">
                <div style="
                    background: linear-gradient(135deg, ${ruta.vehicleColor}, ${ruta.vehicleColor}88);
                    color: white;
                    padding: 8px;
                    margin: -8px -8px 8px -8px;
                    font-weight: bold;
                    font-size: 14px;
                ">
                    🚛 Vehículo ${ruta.vehicleId}
                </div>
                
                <div style="padding: 4px 0;">
                    <div style="margin-bottom: 6px;">
                        <span style="display: inline-block; width: 20px;">📦</span>
                        <strong>${ruta.deliveries}</strong> entregas
                    </div>
                    
                    <div style="margin-bottom: 6px;">
                        <span style="display: inline-block; width: 20px;">📏</span>
                        <strong>${ruta.estimatedDistanceKm} km</strong>
                    </div>
                    
                    ${ruta.algorithm ? `
                        <div style="
                            background: #f8f9fa;
                            border-left: 3px solid #28a745;
                            padding: 4px 8px;
                            margin: 8px 0;
                            font-size: 12px;
                            text-align: left;
                        ">
                            <strong>🤖 Algoritmo:</strong><br>
                            <span style="color: #28a745; font-weight: 600;">${ruta.algorithm}</span>
                        </div>
                    ` : ''}
                </div>
            </div>
        `);

        // Markers para los puntos de entrega (saltando el depósito)
        puntos.forEach((punto, i) => {
            if (i > 0 && i < puntos.length - 1) {
                L.marker(punto, {
                    icon: L.icon({
                        iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-green.png',
                        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
                        iconSize: [20, 32],
                        iconAnchor: [10, 32],
                    })
                }).addTo(routeLayerGroup)
                    .bindPopup(`
                        <div style="text-align: center; font-family: 'Segoe UI', Arial, sans-serif;">
                            <div style="
                                background: linear-gradient(135deg, #28a745, #20c997);
                                color: white;
                                padding: 6px;
                                margin: -8px -8px 6px -8px;
                                font-weight: bold;
                                font-size: 13px;
                            ">
                                ✅ Entrega #${i}
                            </div>
                            
                            <div style="padding: 4px 0; font-size: 12px;">
                                <div style="margin-bottom: 4px;">
                                    <span style="color: ${ruta.vehicleColor}; font-weight: 600;">
                                        🚛 Vehículo ${ruta.vehicleId}
                                    </span>
                                </div>
                                
                                ${ruta.algorithm ? `
                                    <div style="
                                        background: #f8f9fa;
                                        padding: 3px 6px;
                                        border-radius: 3px;
                                        font-size: 11px;
                                        color: #666;
                                    ">
                                        🤖 ${ruta.algorithm}
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    `);
            }
        });
    });

    // ===== CREAR LEYENDA CON INFORMACIÓN DEL ALGORITMO =====
    crearLeyenda(rutasConAlgoritmo, mejorAlgoritmo);

    // Ajustar vista del mapa
    if (rutasConAlgoritmo.length > 0) {
        const allPoints = rutasConAlgoritmo.flatMap(r => r.coordinates);
        const group = new L.featureGroup(allPoints.map(p => L.marker(p)));
        map.fitBounds(group.getBounds().pad(0.1));
    }
}

function mostrarResultados(solucion) {
    const card = document.getElementById("resultadosCard");
    const container = document.getElementById("resultados");

    let html = `
        <div class="row">
            <div class="col-md-6">
                <h6>🏆 Mejor Algoritmo</h6>
                <p><strong>${solucion.best?.algorithm || 'N/A'}</strong></p>
                <p>Costo: <strong>${solucion.best?.cost?.toFixed(2) || 'N/A'}</strong></p>
                <p>Distancia: <strong>${solucion.best?.distanceKm?.toFixed(1) || 'N/A'} km</strong></p>
            </div>
            <div class="col-md-6">
                <h6>📈 Métricas</h6>
                <p>Eficiencia: <strong>${solucion.bestEfficiencyPercent?.toFixed(1) || 'N/A'}%</strong></p>
                <p>Costo por entrega: <strong>${solucion.costPerDelivery?.toFixed(2) || 'N/A'}</strong></p>
                <p>Rutas generadas: <strong>${solucion.routes?.length || 0}</strong></p>
            </div>
        </div>

        <h6 class="mt-3">🔍 Comparación de Algoritmos</h6>
        <div class="table-responsive">
            <table class="table table-sm">
                <thead>
                    <tr>
                        <th>Algoritmo</th>
                        <th>Costo</th>
                        <th>Distancia (km)</th>
                        <th>Tiempo (s)</th>
                    </tr>
                </thead>
                <tbody>
    `;

    solucion.results?.forEach(result => {
        html += `
            <tr${result === solucion.best ? ' class="table-success"' : ''}>
                <td>${result.algorithm}</td>
                <td>${result.cost.toFixed(2)}</td>
                <td>${result.distanceKm.toFixed(1)}</td>
                <td>${result.timeSeconds.toFixed(1)}</td>
            </tr>
        `;
    });

    html += `
                </tbody>
            </table>
        </div>
    `;

    container.innerHTML = html;
    card.style.display = "block";
}

// ===== FUNCIÓN PARA LIMPIAR DATOS Y LEYENDA =====
function cargarDatos() {
    // Generar nuevos datos y mostrarlos
    const numClientes = parseInt(document.getElementById("clientes").value);
    const numVehiculos = parseInt(document.getElementById("vehiculos").value);

    currentClients = generarClientes(numClientes);
    currentVehicles = generarVehiculos(numVehiculos);

    mostrarClientesEnMapa();

    // Limpiar leyenda al cargar nuevos datos
    if (legendControl) {
        map.removeControl(legendControl);
        legendControl = null;
    }

    console.log("🔄 Nuevos datos generados:", {
        clientes: currentClients.length,
        vehiculos: currentVehicles.length
    });
}

// ===== INICIALIZACIÓN =====
window.onload = function () {
    cargarDatos(); // Cargar datos iniciales
};