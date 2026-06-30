// Leaflet.js Blazor Interop
// CRM Filo Servis - Harita Entegrasyonu

window.leafletInterop = {
    maps: {},
    markers: {},
    routes: {},

    // Harita oluştur
    initMap: function (mapId, centerLat, centerLng, zoom) {
        if (this.maps[mapId]) {
            this.maps[mapId].remove();
        }

        const map = L.map(mapId).setView([centerLat, centerLng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);

        this.maps[mapId] = map;
        this.markers[mapId] = [];
        this.routes[mapId] = [];

        return true;
    },

    // Haritayı temizle
    clearMap: function (mapId) {
        if (this.maps[mapId]) {
            // Marker'ları temizle
            if (this.markers[mapId]) {
                this.markers[mapId].forEach(marker => marker.remove());
                this.markers[mapId] = [];
            }
            // Rotaları temizle
            if (this.routes[mapId]) {
                this.routes[mapId].forEach(route => route.remove());
                this.routes[mapId] = [];
            }
        }
    },

    // Haritayı kaldır
    disposeMap: function (mapId) {
        this.clearMap(mapId);
        if (this.maps[mapId]) {
            this.maps[mapId].remove();
            delete this.maps[mapId];
            delete this.markers[mapId];
            delete this.routes[mapId];
        }
    },

    // Marker ekle
    addMarker: function (mapId, lat, lng, title, popupContent, iconColor) {
        if (!this.maps[mapId]) return false;

        const icon = this.createColoredIcon(iconColor || 'blue');
        const marker = L.marker([lat, lng], { icon: icon, title: title }).addTo(this.maps[mapId]);

        if (popupContent) {
            marker.bindPopup(popupContent);
        }

        this.markers[mapId].push(marker);
        return true;
    },

    // Başlangıç marker'ı (yeşil)
    addStartMarker: function (mapId, lat, lng, title, popupContent) {
        return this.addMarker(mapId, lat, lng, title, popupContent, 'green');
    },

    // Bitiş marker'ı (kırmızı)
    addEndMarker: function (mapId, lat, lng, title, popupContent) {
        return this.addMarker(mapId, lat, lng, title, popupContent, 'red');
    },

    // Rota çiz (düz çizgi)
    addRoute: function (mapId, startLat, startLng, endLat, endLng, color, weight) {
        if (!this.maps[mapId]) return false;

        const routeColor = color || '#3388ff';
        const routeWeight = weight || 4;

        const route = L.polyline(
            [[startLat, startLng], [endLat, endLng]],
            { color: routeColor, weight: routeWeight, opacity: 0.8 }
        ).addTo(this.maps[mapId]);

        this.routes[mapId].push(route);
        return true;
    },

    // Güzergah ekle (başlangıç + bitiş marker'ları + rota)
    addGuzergah: function (mapId, startLat, startLng, endLat, endLng, guzergahAdi, baslangicNokta, bitisNokta, color) {
        if (!this.maps[mapId]) return false;

        // Başlangıç marker'ı
        this.addStartMarker(mapId, startLat, startLng, baslangicNokta || 'Başlangıç',
            `<strong>${guzergahAdi}</strong><br><i class="bi bi-geo-alt-fill"></i> ${baslangicNokta || 'Başlangıç Noktası'}`);

        // Bitiş marker'ı
        this.addEndMarker(mapId, endLat, endLng, bitisNokta || 'Bitiş',
            `<strong>${guzergahAdi}</strong><br><i class="bi bi-geo-alt"></i> ${bitisNokta || 'Bitiş Noktası'}`);

        // Rota çiz
        this.addRoute(mapId, startLat, startLng, endLat, endLng, color, 4);

        return true;
    },

    // Çoklu güzergah ekle
    addMultipleGuzergahlar: function (mapId, guzergahlar) {
        if (!this.maps[mapId] || !guzergahlar) return false;

        guzergahlar.forEach(g => {
            if (g.baslangicLat && g.baslangicLng && g.bitisLat && g.bitisLng) {
                this.addGuzergah(
                    mapId,
                    g.baslangicLat,
                    g.baslangicLng,
                    g.bitisLat,
                    g.bitisLng,
                    g.guzergahAdi,
                    g.baslangicNokta,
                    g.bitisNokta,
                    g.rotaRengi
                );
            }
        });

        // Tüm güzergahları göster
        this.fitBounds(mapId);
        return true;
    },

    // Haritayı tüm marker'lara sığdır
    fitBounds: function (mapId) {
        if (!this.maps[mapId] || !this.markers[mapId] || this.markers[mapId].length === 0) return;

        const group = new L.featureGroup(this.markers[mapId]);
        this.maps[mapId].fitBounds(group.getBounds().pad(0.1));
    },

    // Harita merkezini ayarla
    setCenter: function (mapId, lat, lng, zoom) {
        if (!this.maps[mapId]) return;
        this.maps[mapId].setView([lat, lng], zoom || this.maps[mapId].getZoom());
    },

    // Tıklama ile koordinat seç
    enableClickToSelect: function (mapId, dotNetRef) {
        if (!this.maps[mapId]) return;

        this.maps[mapId].on('click', function (e) {
            dotNetRef.invokeMethodAsync('OnMapClicked', e.latlng.lat, e.latlng.lng);
        });
    },

    // Seçim marker'ı göster
    showSelectionMarker: function (mapId, lat, lng, isStart) {
        if (!this.maps[mapId]) return;

        const markerId = isStart ? 'selection_start' : 'selection_end';
        const color = isStart ? 'green' : 'red';

        // Önceki seçim marker'ını kaldır
        if (this[markerId + '_' + mapId]) {
            this[markerId + '_' + mapId].remove();
        }

        const icon = this.createColoredIcon(color);
        const marker = L.marker([lat, lng], { icon: icon }).addTo(this.maps[mapId]);
        this[markerId + '_' + mapId] = marker;
    },

    // Renkli icon oluştur
    createColoredIcon: function (color) {
        const colors = {
            'blue': '#2196F3',
            'green': '#4CAF50',
            'red': '#f44336',
            'orange': '#FF9800',
            'purple': '#9C27B0'
        };

        const iconColor = colors[color] || color;

        return L.divIcon({
            className: 'custom-div-icon',
            html: `<div style="
                background-color: ${iconColor};
                width: 24px;
                height: 24px;
                border-radius: 50% 50% 50% 0;
                transform: rotate(-45deg);
                border: 2px solid white;
                box-shadow: 0 2px 5px rgba(0,0,0,0.3);
            "></div>`,
            iconSize: [24, 24],
            iconAnchor: [12, 24],
            popupAnchor: [0, -24]
        });
    },

    // Harita boyutunu yeniden hesapla
    invalidateSize: function (mapId) {
        if (this.maps[mapId]) {
            setTimeout(() => {
                this.maps[mapId].invalidateSize();
            }, 100);
        }
    },

    // ============ Araç Takip Ek Metodları ============

    // Marker'ları temizle
    clearMarkers: function (mapId) {
        if (this.markers[mapId]) {
            this.markers[mapId].forEach(marker => marker.remove());
            this.markers[mapId] = [];
        }
    },

    // Rotayı temizle
    clearRoute: function (mapId) {
        if (this.routes[mapId]) {
            this.routes[mapId].forEach(route => route.remove());
            this.routes[mapId] = [];
        }
    },

    // Haritayı kaldır (destroyMap alias)
    destroyMap: function (mapId) {
        this.disposeMap(mapId);
    },

    // Harita görünümünü ayarla
    setView: function (mapId, lat, lng, zoom) {
        if (!this.maps[mapId]) return;
        this.maps[mapId].setView([lat, lng], zoom || 15);
    },

    // Rota çiz (çoklu nokta)
    drawRoute: function (mapId, koordinatlar, color) {
        if (!this.maps[mapId] || !koordinatlar || koordinatlar.length < 2) return false;

        const routeColor = color || '#0d6efd';
        const latlngs = koordinatlar.map(k => [k.lat, k.lng]);

        const route = L.polyline(latlngs, {
            color: routeColor,
            weight: 3,
            opacity: 0.8
        }).addTo(this.maps[mapId]);

        if (!this.routes[mapId]) this.routes[mapId] = [];
        this.routes[mapId].push(route);

        return true;
    },

    // Daire bölge çiz (Geofence)
    addCircle: function (mapId, lat, lng, radius, color, fillColor) {
        if (!this.maps[mapId]) return false;

        const circle = L.circle([lat, lng], {
            radius: radius,
            color: color || '#3388ff',
            fillColor: fillColor || '#3388ff',
            fillOpacity: 0.2,
            weight: 2
        }).addTo(this.maps[mapId]);

        if (!this.markers[mapId]) this.markers[mapId] = [];
        this.markers[mapId].push(circle);

        return true;
    },

    // Çokgen bölge çiz (Geofence)
    addPolygon: function (mapId, koordinatlar, color, fillColor) {
        if (!this.maps[mapId] || !koordinatlar || koordinatlar.length < 3) return false;

        const latlngs = koordinatlar.map(k => [k.lat, k.lng]);

        const polygon = L.polygon(latlngs, {
            color: color || '#3388ff',
            fillColor: fillColor || '#3388ff',
            fillOpacity: 0.2,
            weight: 2
        }).addTo(this.maps[mapId]);

        if (!this.markers[mapId]) this.markers[mapId] = [];
        this.markers[mapId].push(polygon);

        return true;
    },

    // Araç marker'ı ekle (yön oku ile)
    addVehicleMarker: function (mapId, lat, lng, color, rotation, popup) {
        if (!this.maps[mapId]) return false;

        const rotationDeg = rotation || 0;
        const iconColor = color || '#28a745';

        const vehicleIcon = L.divIcon({
            className: 'vehicle-marker',
            html: `<div style="
                width: 30px;
                height: 30px;
                background-color: ${iconColor};
                border: 2px solid white;
                border-radius: 50%;
                box-shadow: 0 2px 5px rgba(0,0,0,0.3);
                display: flex;
                align-items: center;
                justify-content: center;
                transform: rotate(${rotationDeg}deg);
            ">
                <svg width="16" height="16" viewBox="0 0 16 16" fill="white">
                    <path d="M8 0L6 6H2l6 10-2-6h4l-2-6H8z" transform="rotate(0 8 8)"/>
                </svg>
            </div>`,
            iconSize: [30, 30],
            iconAnchor: [15, 15],
            popupAnchor: [0, -15]
        });

        const marker = L.marker([lat, lng], { icon: vehicleIcon }).addTo(this.maps[mapId]);

        if (popup) {
            marker.bindPopup(popup);
        }

        if (!this.markers[mapId]) this.markers[mapId] = [];
        this.markers[mapId].push(marker);

        return true;
    }
};
