const fallbackLocations = [
  { id: 'sede-centro-mayor', name: 'Centro Mayor', address: 'Calle 38A Sur # 34D-51, Antonio Nariño, Bogotá.', latitude: 4.5948, longitude: -74.1434 },
  { id: 'sede-santa-fe', name: 'Centro Comercial Santa Fe', address: 'Autopista Norte con Calle 183, Bogotá.', latitude: 4.7612, longitude: -74.0451 },
  { id: 'sede-plaza-central', name: 'Plaza Central', address: 'Carrera 65 # 11-50, Puente Aranda, Bogotá.', latitude: 4.6277, longitude: -74.1307 },
  { id: 'sede-mallplaza-nqs', name: 'Mallplaza NQS', address: 'Carrera 30 # 19, Los Mártires, Bogotá.', latitude: 4.6198, longitude: -74.0772 },
  { id: 'sede-nuestro-bogota', name: 'Nuestro Bogotá', address: 'Av. Carrera 86 # 55A-75, Engativá, Bogotá.', latitude: 4.6958, longitude: -74.1064 }
];
const services = [['⌁', 'WiFi'], ['♨', 'Cafetería'], ['▣', 'Parqueadero'], ['♢', 'Seguridad 24/7'], ['◫', 'Salas insonorizadas'], ['⌂', 'Zona lounge']];
function renderLocations(locations) {
  document.querySelector('#location-cards').innerHTML = locations.map(location => `<article class="location-detail" data-location="${location.id}"><div class="location-detail-photo"></div><div class="location-detail-content"><h2>CoSpace ${location.name}</h2><p class="location-address">⌖ ${location.address}</p><p class="location-hours">◷ Lunes a sábado, 7:00 a.m. - 9:00 p.m.</p><div class="service-list">${services.map(([icon, label]) => `<span class="service"><span class="service-icon">${icon}</span>${label}</span>`).join('')}</div><a class="button" href="reservas.html?location=${encodeURIComponent(location.name)}">Ver disponibilidad <span>→</span></a></div></article>`).join('');
  initMap(locations);
}
function initMap(locations) {
  if (!window.L) return;
  const map = L.map('city-map', { scrollWheelZoom: false }).setView([4.658, -74.095], 11);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '&copy; OpenStreetMap contributors' }).addTo(map);
  const bounds = [];
  locations.forEach(location => {
    const point = [location.latitude, location.longitude]; bounds.push(point);
    const marker = L.marker(point, { icon: L.divIcon({ className: 'map-marker', html: '<span class="map-pin"></span>', iconSize: [16, 16], iconAnchor: [8, 8] }) }).addTo(map);
    marker.bindPopup(`<strong>CoSpace ${location.name}</strong><small>${location.address}</small><small><a href="reservas.html?location=${encodeURIComponent(location.name)}">Ver disponibilidad</a></small>`);
    marker.on('click', () => document.querySelector(`[data-location="${location.id}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'center' }));
  });
  map.fitBounds(bounds, { padding: [30, 30] });
}
async function loadLocations() {
  try {
    const response = await fetch(`${window.CoSpaceConfig.apiBaseUrl}/sedes`);
    if (!response.ok) throw new Error('API unavailable');
    renderLocations(await response.json());
  } catch { renderLocations(fallbackLocations); }
}
document.querySelector('#menu-toggle').addEventListener('click', () => document.querySelector('#site-nav').classList.toggle('open'));
document.querySelectorAll('#site-nav a').forEach(link => link.addEventListener('click', () => document.querySelector('#site-nav').classList.remove('open')));
loadLocations();
