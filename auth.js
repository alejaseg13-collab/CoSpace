const API_URL = `${window.CoSpaceConfig.apiBaseUrl}/auth`;
const params = new URLSearchParams(location.search);
const mode = params.get('mode') === 'login' ? 'login' : 'register';
const plan = params.get('plan');
const form = document.querySelector('#auth-form');
const title = document.querySelector('#auth-title');
const description = document.querySelector('#auth-description');
const nameField = document.querySelector('#name-field');
const phoneField = document.querySelector('#phone-field');
const locationField = document.querySelector('#location-field');
const confirmField = document.querySelector('#confirm-field');
const termsField = document.querySelector('#terms-field');
const rememberField = document.querySelector('#remember-field');
const recoveryLink = document.querySelector('#recovery-link');
const submit = document.querySelector('#auth-submit');
const switchText = document.querySelector('#auth-switch');
const errorBox = document.querySelector('#form-error');
const demoAccounts = document.querySelector('#demo-accounts');
const sessionUser = JSON.parse(localStorage.getItem('cospace-user') || 'null');
if (sessionUser && sessionUser.rol) localStorage.setItem('cospace-authenticated', 'true');
if (mode === 'login') {
  title.textContent = 'Iniciar sesión'; description.textContent = 'Bienvenido de nuevo. Ingresa a tu cuenta para continuar.';
  [nameField, phoneField, locationField, confirmField, termsField].forEach(field => field.remove());
  rememberField.hidden = false; recoveryLink.hidden = false; submit.innerHTML = 'Iniciar sesión <span>→</span>';
  switchText.innerHTML = '¿No tienes una cuenta? <a href="auth.html?mode=register">Regístrate</a>';
} else {
  demoAccounts.remove();
  title.textContent = 'Crea tu cuenta'; description.textContent = plan ? `Regístrate para comenzar con el plan ${plan}.` : 'Regístrate para reservar espacios y elegir tu plan.';
  rememberField.hidden = true; recoveryLink.hidden = true; submit.innerHTML = 'Registrarme <span>→</span>';
  switchText.innerHTML = '¿Ya tienes una cuenta? <a href="auth.html?mode=login">Inicia sesión</a>';
  loadLocations();
}
document.querySelectorAll('[data-demo-email]').forEach(button => button.addEventListener('click', () => {
  form.elements.correo.value = button.dataset.demoEmail;
  form.elements.contrasena.value = 'Demo1234!';
  showError('Cuenta de ejemplo cargada. Pulsa Iniciar sesión.');
  errorBox.classList.add('demo-notice');
}));
async function loadLocations() {
  const select = document.querySelector('#sede-preferida');
  try {
    const response = await fetch(`${window.CoSpaceConfig.apiBaseUrl}/sedes`);
    const locations = response.ok ? await response.json() : [];
    locations.forEach(location => select.insertAdjacentHTML('beforeend', `<option value="${location.id}">${location.name}</option>`));
  } catch {
    [['sede-centro-mayor','Centro Mayor'],['sede-santa-fe','Centro Comercial Santa Fe'],['sede-plaza-central','Plaza Central'],['sede-mallplaza-nqs','Mallplaza NQS'],['sede-nuestro-bogota','Nuestro Bogotá']].forEach(([id,name]) => select.insertAdjacentHTML('beforeend', `<option value="${id}">${name}</option>`));
  }
}
function showError(message) { errorBox.textContent = message; errorBox.classList.add('show'); }
function validate(data) {
  if (!data.correo || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.correo)) return 'Ingresa un correo electrónico válido.';
  if (!data.contrasena || data.contrasena.length < 8) return 'La contraseña debe tener mínimo 8 caracteres.';
  if (mode === 'register') {
    if (!data.nombre || !data.telefono || !data.sedePreferidaId) return 'Completa todos los campos obligatorios.';
    if (data.contrasena !== data.confirmarContrasena) return 'Las contraseñas no coinciden.';
    if (!data.aceptaTerminos) return 'Debes aceptar los términos y condiciones.';
  }
  return '';
}
form.addEventListener('submit', async event => {
  event.preventDefault(); errorBox.classList.remove('show');
  const formData = new FormData(form);
  const data = Object.fromEntries(formData.entries());
  data.aceptaTerminos = formData.get('aceptaTerminos') === 'on'; data.recuerdame = formData.get('recuerdame') === 'on';
  const validationError = validate(data); if (validationError) return showError(validationError);
  submit.disabled = true; submit.innerHTML = 'Procesando...';
  try {
    const response = await fetch(`${API_URL}/${mode}`, { method: 'POST', headers: {'Content-Type':'application/json'}, body: JSON.stringify(data) });
    if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.error || (response.status === 409 ? 'Este correo ya está registrado.' : 'No pudimos completar la solicitud.')); }
    const user = await response.json(); completeAuth(user);
  } catch (error) {
    if (error instanceof TypeError) {
      if (mode === 'login') return showError('La API no está disponible. Inicia el backend para autenticar tu cuenta.');
      const localUsers = JSON.parse(localStorage.getItem('cospace-users') || '[]');
      if (localUsers.some(user => user.correo.toLowerCase() === data.correo.toLowerCase())) return showError('Este correo ya está registrado.');
      const user = { id: crypto.randomUUID(), nombre: data.nombre, correo: data.correo.toLowerCase(), rol: 'Miembro', planId: 'plan-basico', sedePreferidaId: data.sedePreferidaId };
      localUsers.push(user); localStorage.setItem('cospace-users', JSON.stringify(localUsers)); completeAuth(user); return;
    }
    showError(error.message);
  } finally { submit.disabled = false; if (mode === 'login') submit.innerHTML = 'Iniciar sesión <span>→</span>'; }
});
function completeAuth(user) { localStorage.setItem('cospace-user', JSON.stringify(user)); localStorage.setItem('cospace-authenticated', 'true'); location.href = user.rol === 'Administrador' || user.rol === 'admin' || user.rol === 1 ? 'admin.html' : 'perfil.html'; }
document.querySelectorAll('.password-toggle').forEach(toggle => toggle.addEventListener('click', () => { const input = document.querySelector(`#${toggle.dataset.target}`); input.type = input.type === 'password' ? 'text' : 'password'; toggle.textContent = input.type === 'password' ? '◉' : '◌'; }));
recoveryLink.addEventListener('click', event => { event.preventDefault(); showError('Te enviaremos un enlace de recuperación cuando Firebase Authentication esté conectado.'); });
document.querySelector('#google-button').addEventListener('click', () => showError('El acceso con Google estará disponible al conectar Firebase Authentication.'));
