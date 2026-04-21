// ─────────────────────────────────────────────────────────────────────────────
// walkmap.js  –  WalkMap Leaflet helpers
// ─────────────────────────────────────────────────────────────────────────────
// Public surface (called from Blazor via JS interop):
//   wmGetLocation()                     → Promise<[lat, lng]>
//   wmInitMap(lat, lng)                 → void
//   wmDrawRoute(points)                 → void   (array of [lat,lng])
//   wmSetDotNetRef(ref)                 → void
//   wmStartWalkTracking(routePoints)    → void
//   wmDestroyMap()                      → void

(function () {
    "use strict";

    // ── State ──────────────────────────────────────────────────────────────────
    let _map = null;
    let _dotNet = null;          // DotNetObjectReference from Blazor
    let _watchId = null;         // navigator.geolocation watch handle
    let _arrowMarker = null;     // rotatable heading marker
    let _trailLine = null;       // live-walk polyline
    let _routeLine = null;       // pre-planned route polyline
    let _trailCoords = [];       // [[lat,lng], …] collected so far
    let _totalMeters = 0;        // accumulated distance (noise-filtered)
    let _lastValidPos = null;    // { lat, lng } last accepted position
    let _stepCount = 0;
    let _stepSensor = null;      // Sensor API handle (if available)

    // Tuning constants
    const MIN_MOVE_METERS = 5;   // ignore GPS jitter below this threshold
    const TRAIL_COLOR = "#3b82f6";
    const ROUTE_COLOR = "#94a3b8";

    // ── Helpers ────────────────────────────────────────────────────────────────

    /** Haversine distance in metres between two [lat,lng] pairs. */
    function haversine(lat1, lon1, lat2, lon2) {
        const R = 6_371_000;
        const φ1 = lat1 * Math.PI / 180, φ2 = lat2 * Math.PI / 180;
        const Δφ = (lat2 - lat1) * Math.PI / 180;
        const Δλ = (lon2 - lon1) * Math.PI / 180;
        const a = Math.sin(Δφ / 2) ** 2
            + Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) ** 2;
        return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    /** Bearing in degrees (0 = North, clockwise) from point A to point B. */
    function bearing(lat1, lon1, lat2, lon2) {
        const φ1 = lat1 * Math.PI / 180, φ2 = lat2 * Math.PI / 180;
        const Δλ = (lon2 - lon1) * Math.PI / 180;
        const y = Math.sin(Δλ) * Math.cos(φ2);
        const x = Math.cos(φ1) * Math.sin(φ2)
            - Math.sin(φ1) * Math.cos(φ2) * Math.cos(Δλ);
        return ((Math.atan2(y, x) * 180 / Math.PI) + 360) % 360;
    }

    /** Build (or rebuild) a Leaflet DivIcon showing a directional arrow. */
    function makeArrowIcon(headingDeg) {
        return L.divIcon({
            className: "",          // no default Leaflet styling
            iconSize: [36, 36],
            iconAnchor: [18, 18],
            html: `
        <div style="
          width:36px; height:36px;
          display:flex; align-items:center; justify-content:center;
          transform: rotate(${headingDeg}deg);
          transition: transform 0.4s ease;
        ">
          <svg viewBox="0 0 36 36" width="36" height="36" xmlns="http://www.w3.org/2000/svg">
            <!-- Outer circle -->
            <circle cx="18" cy="18" r="17" fill="#2563eb" fill-opacity="0.15"
                    stroke="#2563eb" stroke-width="1.5"/>
            <!-- Arrow pointing up (North when heading=0) -->
            <polygon points="18,5 25,28 18,23 11,28"
                     fill="#2563eb" stroke="#fff" stroke-width="1.5"
                     stroke-linejoin="round"/>
          </svg>
        </div>`
        });
    }

    /** Ensure the map container exists and Leaflet is loaded. */
    function ensureMap(lat, lng) {
        if (_map) return;
        const el = document.getElementById("wm-map");
        if (!el) { console.warn("wmMap: #wm-map not found"); return; }

        _map = L.map("wm-map").setView([lat, lng], 15);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "© OpenStreetMap contributors",
            maxZoom: 19,
        }).addTo(_map);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /** Return user's current position as [lat, lng]. */
    window.wmGetLocation = function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error("Geolocation is not supported by this browser."));
                return;
            }
            navigator.geolocation.getCurrentPosition(
                pos => resolve([pos.coords.latitude, pos.coords.longitude]),
                err => reject(new Error(err.message)),
                { enableHighAccuracy: true, timeout: 15_000, maximumAge: 0 }
            );
        });
    };

    /** Initialise (or re-centre) the map without starting tracking. */
    window.wmInitMap = function (lat, lng) {
        ensureMap(lat, lng);
        if (_map) _map.setView([lat, lng], 15);
    };

    /** Draw a static planned-route polyline (grey). */
    window.wmDrawRoute = function (points) {
        if (!_map) return;
        if (_routeLine) { _routeLine.remove(); _routeLine = null; }

        const latlngs = points.map(p => [p[0], p[1]]);
        _routeLine = L.polyline(latlngs, {
            color: ROUTE_COLOR,
            weight: 4,
            opacity: 0.6,
            dashArray: "6 6",
        }).addTo(_map);

        if (latlngs.length) _map.fitBounds(_routeLine.getBounds(), { padding: [40, 40] });
    };

    /** Store the Blazor DotNetObjectReference so JS can call back into C#. */
    window.wmSetDotNetRef = function (ref) {
        _dotNet = ref;
    };

    /**
     * Begin live GPS tracking.
     * @param {Array<[number,number]>} routePoints  Pre-planned route [[lat,lng],…]
     */
    window.wmStartWalkTracking = function (routePoints) {
        if (!navigator.geolocation) {
            console.error("Geolocation unavailable");
            return;
        }

        // ── Initialise with the first route point while we wait for GPS ──────────
        const startPt = routePoints && routePoints.length
            ? [routePoints[0][0], routePoints[0][1]]
            : [0, 0];

        ensureMap(startPt[0], startPt[1]);

        // Draw the planned route (dashed grey)
        if (routePoints && routePoints.length) {
            window.wmDrawRoute(routePoints);
        }

        // Initialise the live trail polyline (blue)
        _trailCoords = [];
        _totalMeters = 0;
        _lastValidPos = null;

        _trailLine = L.polyline([], {
            color: TRAIL_COLOR,
            weight: 5,
            opacity: 0.85,
        }).addTo(_map);

        // Place the arrow marker at the start
        _arrowMarker = L.marker([startPt[0], startPt[1]], {
            icon: makeArrowIcon(0),
            zIndexOffset: 1000,
        }).addTo(_map);

        // ── Step counter (Sensor API, Chrome / Android) ───────────────────────
        _tryStartStepSensor();

        // ── Watch position ────────────────────────────────────────────────────
        _watchId = navigator.geolocation.watchPosition(
            _onPosition,
            _onGpsError,
            { enableHighAccuracy: true, timeout: 20_000, maximumAge: 1_000 }
        );
    };

    /** Tear down the map and stop all watchers. */
    window.wmDestroyMap = function () {
        if (_watchId !== null) {
            navigator.geolocation.clearWatch(_watchId);
            _watchId = null;
        }
        if (_stepSensor) {
            try { _stepSensor.stop(); } catch (_) { }
            _stepSensor = null;
        }
        if (_map) {
            _map.remove();
            _map = null;
        }
        _arrowMarker = null;
        _trailLine = null;
        _routeLine = null;
        _trailCoords = [];
        _totalMeters = 0;
        _lastValidPos = null;
        _dotNet = null;
    };

    // ── Internal: GPS position handler ────────────────────────────────────────

    let _lastPositionTime = 0;   // ms timestamp of last accepted fix

    function _onPosition(pos) {
        const { latitude: lat, longitude: lng, speed, heading } = pos.coords;
        const now = pos.timestamp;

        // ── Minimum-movement filter ───────────────────────────────────────────
        if (_lastValidPos !== null) {
            const dist = haversine(_lastValidPos.lat, _lastValidPos.lng, lat, lng);

            if (dist < MIN_MOVE_METERS) {
                // Still update the arrow heading if the device reports one
                if (heading !== null && _arrowMarker) {
                    _arrowMarker.setIcon(makeArrowIcon(heading));
                }
                return;   // skip — GPS jitter
            }

            // ── Accumulate distance ─────────────────────────────────────────────
            _totalMeters += dist;
        }

        // ── Compute heading from movement if device doesn't provide one ───────
        let displayHeading = heading ?? 0;
        if ((heading === null || heading === undefined) && _lastValidPos) {
            displayHeading = bearing(_lastValidPos.lat, _lastValidPos.lng, lat, lng);
        }

        _lastValidPos = { lat, lng };
        _lastPositionTime = now;

        // ── Update map ────────────────────────────────────────────────────────
        const latlng = [lat, lng];
        _trailCoords.push(latlng);
        _trailLine.setLatLngs(_trailCoords);

        // Move + rotate the arrow marker
        _arrowMarker.setLatLng(latlng);
        _arrowMarker.setIcon(makeArrowIcon(displayHeading));

        // Keep the user centred on the map
        _map.panTo(latlng, { animate: true, duration: 0.5 });

        // ── Notify Blazor ─────────────────────────────────────────────────────
        if (_dotNet) {
            _dotNet.invokeMethodAsync("UpdateDistance", _totalMeters);
            _dotNet.invokeMethodAsync("UpdatePosition", lat, lng);

            // Speed: prefer GPS-reported value, otherwise derive from distance/time
            let kmh = 0;
            if (speed !== null && speed !== undefined && speed >= 0) {
                kmh = speed * 3.6;
            }
            _dotNet.invokeMethodAsync("UpdateSpeed", kmh);
        }
    }

    function _onGpsError(err) {
        console.warn("GPS error:", err.message);
    }

    // ── Internal: Step sensor ─────────────────────────────────────────────────

    function _tryStartStepSensor() {
        // Sensor API is Chrome/Android only; fail silently on unsupported devices.
        if (typeof Accelerometer === "undefined") return;

        try {
            // Pedometer-style step detection via accelerometer magnitude peaks
            const accel = new Accelerometer({ frequency: 20 });
            let _prevMag = 0;
            let _inPeak = false;
            const STEP_THRESHOLD = 12;   // m/s² — tune to taste

            accel.addEventListener("reading", () => {
                const mag = Math.sqrt(accel.x ** 2 + accel.y ** 2 + accel.z ** 2);
                if (!_inPeak && mag > STEP_THRESHOLD) {
                    _inPeak = true;
                    _stepCount++;
                    if (_dotNet) _dotNet.invokeMethodAsync("UpdateSteps", _stepCount);
                } else if (mag < STEP_THRESHOLD - 2) {
                    _inPeak = false;
                }
                _prevMag = mag;
            });

            accel.addEventListener("error", () => { });
            accel.start();
            _stepSensor = accel;
        } catch (_) {
            // Sensor API permission denied or not available — ignore silently
        }
    }

})();