window.wmMap = null;
window.wmRouteLayer = null;
window.wmCurrentMarker = null;
window.wmDotNetRef = null;

window.wmGetLocation = async () => {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject("Geolocation not supported");
            return;
        }
        navigator.geolocation.getCurrentPosition(
            (pos) => resolve([pos.coords.latitude, pos.coords.longitude]),
            (err) => reject(err.message || "Geolocation error"),
            { enableHighAccuracy: true }
        );
    });
};

window.wmInitMap = (lat, lng) => {
    if (window.wmMap) {
        window.wmMap.remove();
        window.wmMap = null;
    }
    window.wmMap = L.map('wm-map').setView([lat, lng], 15);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(window.wmMap);
};

window.wmDrawRoute = (pts) => {
    if (!pts || pts.length === 0) return;
    const latlngs = pts
        .filter(p => p && p.length === 2 && p[0] != null && p[1] != null)
        .map(p => [p[0], p[1]]);
    if (!latlngs.length) return;
    if (window.wmRouteLayer) {
        window.wmRouteLayer.remove();
    }
    window.wmRouteLayer = L.polyline(latlngs, { color: 'blue', weight: 5 }).addTo(window.wmMap);
    window.wmMap.fitBounds(window.wmRouteLayer.getBounds());
};

window.wmSetDotNetRef = (ref) => {
    window.wmDotNetRef = ref;
};

window.wmStartWalkTracking = async (routePts) => {
    if (!routePts || routePts.length === 0) return;

    // Destroy any existing map first
    if (window.wmMap) {
        window.wmMap.remove();
        window.wmMap = null;
        window.wmRouteLayer = null;
        window.wmCurrentMarker = null;
    }

    const center = routePts[0];
    window.wmMap = L.map('wm-map').setView([center[0], center[1]], 15);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(window.wmMap);

    window.wmDrawRoute(routePts);

    if (!navigator.geolocation) {
        console.error("Geolocation not supported");
        return;
    }

    let totalDistance = 0;
    let lastPos = null;

    navigator.geolocation.watchPosition(pos => {
        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;

        if (!window.wmCurrentMarker) {
            window.wmCurrentMarker = L.marker([lat, lng]).addTo(window.wmMap);
        } else {
            window.wmCurrentMarker.setLatLng([lat, lng]);
        }

        if (lastPos) {
            const prev = L.latLng(lastPos[0], lastPos[1]);
            const curr = L.latLng(lat, lng);
            totalDistance += prev.distanceTo(curr);
        }
        lastPos = [lat, lng];

        window.wmMap.setView([lat, lng]);

        if (window.wmDotNetRef) {
            window.wmDotNetRef.invokeMethodAsync('UpdateDistance', totalDistance);
        }
    }, err => console.error(err), { enableHighAccuracy: true });
};

window.wmDestroyMap = () => {
    if (window.wmMap) {
        window.wmMap.remove();
        window.wmMap = null;
        window.wmRouteLayer = null;
        window.wmCurrentMarker = null;
        window.wmDotNetRef = null;
    }
};