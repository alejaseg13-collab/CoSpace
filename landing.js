const locations = [
  ['Centro Mayor', 'Calle 38A Sur # 34D-51, Antonio Nariño'],
  ['Centro Comercial Santa Fe', 'Autopista Norte con Calle 183'],
  ['Plaza Central', 'Carrera 65 # 11-50, Puente Aranda'],
  ['Mallplaza NQS', 'Carrera 30 # 19, Los Mártires'],
  ['Nuestro Bogotá', 'Av. Carrera 86 # 55A-75, Engativá']
];
const categories = [
  { name: 'Puestos', label: 'Escritorio flexible', description: 'Trabaja en un ambiente colaborativo y dinámico.', price: 12000, image: 'https://images.unsplash.com/photo-1497366754035-f200968a6e72?auto=format&fit=crop&w=700&q=85', icon: '▤', filter: 'Escritorio flexible' },
  { name: 'Oficinas', label: 'Oficina privada', description: 'Privacidad y comodidad para concentrarte.', price: 35000, image: 'https://images.unsplash.com/photo-1497366412874-3415097a27e7?auto=format&fit=crop&w=700&q=85', icon: '▦', filter: 'Oficina privada' },
  { name: 'Cubículos', label: 'Oficina privada', description: 'Tu propio espacio flexible para equipos.', price: 55000, image: 'https://images.unsplash.com/photo-1497366811360-8b9f1a57c69d?auto=format&fit=crop&w=700&q=85', icon: '▦', filter: 'Oficina privada' },
  { name: 'Salas de reunión', label: 'Sala de reunión', description: 'Ideales para tus juntas y presentaciones.', price: 70000, image: 'https://images.unsplash.com/photo-1497366216548-37526070297c?auto=format&fit=crop&w=700&q=85', icon: '♧', filter: 'Sala de reunión' }
];
const plans = [
  { name: 'Básico', price: 320000, caption: 'Ideal para freelancers', benefits: ['Puesto flexible ilimitado en cualquier sede', '5h de salas de reunión'] },
  { name: 'Pro', price: 580000, caption: 'Para profesionales y equipos pequeños', benefits: ['Puesto flexible ilimitado', 'Oficina privada por horas con descuento', '20h de salas de reunión'], popular: true },
  { name: 'Empresarial', price: 1450000, caption: 'Para empresas y equipos grandes', benefits: ['Oficina privada dedicada', 'Salas ilimitadas', 'Soporte prioritario'] }
];
const money = value => new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(value);
const categoryGrid = document.querySelector('#category-grid');
categoryGrid.innerHTML = categories.map(category => `<a class="category-card" href="reservas.html?tipo=${encodeURIComponent(category.filter)}"><div class="category-image" style="background-image:url('${category.image}')"></div><div class="category-body"><h3>${category.name}</h3><p>${category.description}</p><span class="price">Desde <strong>${money(category.price)}</strong> / hora</span></div></a>`).join('');
document.querySelector('#location-grid').innerHTML = locations.map(([name, address]) => `<a class="location-card" href="nosotros.html?sede=${encodeURIComponent(name)}"><span class="location-pin">⌖</span><h3>${name}</h3><p>${address}<br>Bogotá</p></a>`).join('');
document.querySelector('#plans-grid').innerHTML = plans.map((plan, index) => `<article class="plan-card ${plan.popular ? 'featured' : ''}">${plan.popular ? '<span class="plan-popular">Más popular</span>' : ''}<div class="plan-icon">${index === 0 ? '♙' : index === 1 ? '♧' : '▥'}</div><h3>${plan.name}</h3><p>${plan.caption}</p><div class="plan-price">${money(plan.price)} <small>/ mes</small></div><div class="benefits">${plan.benefits.map(benefit => `<span>${benefit}</span>`).join('')}</div><a class="button" href="auth.html?mode=register&plan=${encodeURIComponent(plan.name)}">Elegir plan</a></article>`).join('');
const menuToggle = document.querySelector('#menu-toggle');
const nav = document.querySelector('#site-nav');
menuToggle.addEventListener('click', () => nav.classList.toggle('open'));
nav.querySelectorAll('a').forEach(link => link.addEventListener('click', () => nav.classList.remove('open')));
