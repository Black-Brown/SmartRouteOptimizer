document.addEventListener("DOMContentLoaded", () => {
    const map = L.map("map").setView([18.4861, -69.9312], 11); // Santo Domingo
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png").addTo(map);

    document.getElementById("btnOptimize").addEventListener("click", async () => {
        const addresses = document.getElementById("inputAddresses").value
            .split("\n").filter(x => x.trim() !== "");

        const vehicleCount = parseInt(document.getElementById("vehicleCount").value);

        const request = {
            addresses: addresses,
            vehicleCount: vehicleCount
        };

        const response = await fetch("/api/optimize", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request)
        });

        const data = await response.json();
        document.getElementById("results").textContent = JSON.stringify(data, null, 2);

        // limpiar mapa
        map.eachLayer((layer) => {
            if (layer instanceof L.Polyline || layer instanceof L.Marker) {
                map.removeLayer(layer);
            }
        });

        // pintar rutas
        data.vehicleRoutes.forEach((route, idx) => {
            const latlngs = route.stops.map(s => [s.lat, s.lng]);

            L.polyline(latlngs, { color: idx % 2 === 0 ? "blue" : "green" })
                .addTo(map)
                .bindPopup(`Vehículo ${idx + 1}`);

            latlngs.forEach(coord => L.marker(coord).addTo(map));
        });
    });
});
