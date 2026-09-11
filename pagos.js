window.CoSpaceConfig = window.CoSpaceConfig || { apiBaseUrl: window.location.hostname === 'localhost' || window.location.protocol === 'file:' ? 'http://localhost:5050/api' : '/api' };
const API = window.CoSpaceConfig.apiBaseUrl;
const params = new URLSearchParams(location.search);
const user = JSON.parse(localStorage.getItem('cospace-user') || 'null') || { id: 'demo-member', nombre: 'Sebastián Gil' };
const value = key => params.get(key) || '';
const money = amount => new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(amount);
let method = 0;
let history = [];
const subtotal = Number(value('subtotal') || Math.round(Number(value('monto') || 20000) / 1.19));
const total = Number(value('monto') || Math.round(subtotal * 1.19));

document.querySelector('#pay-user-name').textContent = user.nombre;
document.querySelector('#reservation-space').textContent = value('espacio') || 'Sala de reunión 01';
document.querySelector('#reservation-location').textContent = value('sede') || 'Piso 3 · Sala A';
document.querySelector('#reservation-date').textContent = value('fecha') ? new Date(`${value('fecha')}T12:00:00`).toLocaleDateString('es-CO', { day: 'numeric', month: 'short', year: 'numeric' }) : '15 abr. 2025';
document.querySelector('#reservation-time').textContent = value('inicio') ? `${value('inicio')} - ${value('fin')}` : '09:00 - 11:00';
document.querySelector('#reservation-duration').textContent = `${Math.max(1, Number((value('fin') || '11:00').slice(0, 2)) - Number((value('inicio') || '09:00').slice(0, 2)))} horas`;
document.querySelector('#reservation-subtotal').textContent = money(subtotal);
document.querySelector('#pay-total').textContent = money(total);

function statusLabel(status) { return status === 0 ? ['Pagado', 'paid'] : status === 1 ? ['Pendiente', 'pending'] : ['Vencido', 'expired']; }
function formatDate(date) { return new Date(date).toLocaleDateString('es-CO', { day: 'numeric', month: 'short', year: 'numeric' }); }
function renderHistory(items) {
  history = items;
  document.querySelector('#invoice-body').innerHTML = items.map(item => {
    const [label, className] = statusLabel(item.pago.estado);
    return `<tr><td><strong>${item.pago.numeroFactura || 'Pendiente'}</strong></td><td>${formatDate(item.pago.fecha)}</td><td>${item.espacio.nombre}<br><small>${item.espacio.ubicacion}</small></td><td>${money(item.pago.monto)}</td><td><span class="invoice-status ${className}">✓ &nbsp; ${label}</span></td><td><button class="download-row" data-payment="${item.pago.id}">▧ &nbsp; Ver factura</button></td></tr>`;
  }).join('') || '<tr><td colspan="6">No tienes facturas todavía.</td></tr>';
}
async function loadHistory() {
  try {
    const response = await fetch(`${API}/pagos/${user.id}`);
    if (!response.ok) throw new Error();
    renderHistory(await response.json());
  } catch {
    renderHistory([{ pago: { id: 'demo-payment', numeroFactura: 'FAC-001245', fecha: '2026-03-10T00:00:00Z', monto: 580000, estado: 0 }, espacio: { nombre: 'Plan Pro (Membresía)', ubicacion: 'CoSpace' } }]);
  }
}
document.querySelectorAll('.method-tab').forEach((tab, index) => tab.addEventListener('click', () => {
  method = index;
  document.querySelectorAll('.method-tab').forEach(item => item.classList.remove('active'));
  tab.classList.add('active');
  document.querySelectorAll('.method-fields').forEach(item => item.classList.remove('active'));
  document.querySelector(`#${tab.dataset.method}-fields`).classList.add('active');
}));
document.querySelector('#payment-form').addEventListener('submit', async event => {
  event.preventDefault();
  const error = document.querySelector('#pay-error');
  error.classList.remove('show');
  const reservationId = value('reservaId');
  if (!reservationId || reservationId.startsWith('local-')) { error.textContent = 'La reserva no está conectada a la API. Inicia el backend para confirmar el pago.'; error.classList.add('show'); return; }
  const button = document.querySelector('#pay-button');
  button.disabled = true;
  button.textContent = 'Procesando pago...';
  try {
    const response = await fetch(`${API}/pagos/confirmar`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ usuarioId: user.id, reservaId: reservationId, metodo: method }) });
    if (!response.ok) throw new Error((await response.json()).error || 'No se pudo procesar el pago.');
    alert('Pago realizado. Factura generada correctamente.');
    location.href = 'perfil.html';
  } catch (error) {
    document.querySelector('#pay-error').textContent = error.message;
    document.querySelector('#pay-error').classList.add('show');
    button.disabled = false;
    button.innerHTML = `▤ &nbsp; Pagar ahora <span>${money(total)}</span> →`;
  }
});
async function downloadPayment(id) {
  try {
    const response = await fetch(`${API}/pagos/${user.id}/${id}/factura`);
    if (!response.ok) throw new Error();
    const link = document.createElement('a');
    link.href = URL.createObjectURL(await response.blob());
    link.download = 'factura.txt';
    link.click();
  } catch {
    const item = history.find(entry => entry.pago.id === id);
    const text = `Factura ${item?.pago.numeroFactura || 'CoSpace'}\n${item?.espacio.nombre || ''}\nMonto: ${money(item?.pago.monto || 0)}`;
    const link = document.createElement('a');
    link.href = URL.createObjectURL(new Blob([text], { type: 'text/plain' }));
    link.download = 'factura.txt';
    link.click();
  }
}
document.querySelector('#invoice-body').addEventListener('click', event => { const button = event.target.closest('[data-payment]'); if (button) downloadPayment(button.dataset.payment); });
document.querySelector('#download-all').addEventListener('click', () => history.forEach(item => downloadPayment(item.pago.id)));
document.querySelector('#logout').addEventListener('click', () => { localStorage.removeItem('cospace-user'); localStorage.removeItem('cospace-authenticated'); });
loadHistory();
