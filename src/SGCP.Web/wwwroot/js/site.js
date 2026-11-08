(() => {
  const toggle = document.querySelector('.menu-toggle');
  const sidebar = document.getElementById('navegacion');
  const shade = document.getElementById('sombra');
  function menu(open) { if (!sidebar || !toggle || !shade) return; sidebar.classList.toggle('open', open); toggle.setAttribute('aria-expanded', String(open)); shade.hidden = !open; if (!open) toggle.focus(); }
  toggle?.addEventListener('click', () => menu(!sidebar.classList.contains('open')));
  shade?.addEventListener('click', () => menu(false));
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && sidebar?.classList.contains('open')) menu(false); });
  document.querySelectorAll('.show-password').forEach(b => b.addEventListener('click', () => { const input = b.previousElementSibling; input.type = input.type === 'password' ? 'text' : 'password'; b.textContent = input.type === 'password' ? 'Ver' : 'Ocultar'; b.setAttribute('aria-label', input.type === 'password' ? 'Mostrar contraseña' : 'Ocultar contraseña'); }));
  const rows = document.getElementById('detalles');
  function calcular() {
    if (!rows) return;
    let total = 0;
    [...rows.children].forEach((row, index) => {
      row.querySelectorAll('[data-prop]').forEach(input => input.name = `Detalles[${index}].${input.dataset.prop}`);
      const select = row.querySelector('select'); select.id = `producto-${index}`; row.querySelector('label').htmlFor = select.id;
      const subtotal = Number(row.querySelector('[data-prop="Cantidad"]').value) * Number(row.querySelector('[data-prop="PrecioUnitario"]').value);
      row.querySelector('.subtotal').textContent = Number.isFinite(subtotal) ? subtotal.toFixed(2) : '0.00'; total += Number.isFinite(subtotal) ? Math.round((subtotal + Number.EPSILON) * 100) / 100 : 0;
      row.querySelector('.remove-row').disabled = rows.children.length === 1;
    });
    document.getElementById('total-cotizacion').textContent = total.toFixed(2);
  }
  rows?.addEventListener('input', calcular);
  rows?.addEventListener('change', e => {
    if (e.target.matches('.producto-select')) { const option = e.target.selectedOptions[0]; const row = e.target.closest('tr'); row.querySelector('[data-prop="Unidad"]').value = option.dataset.unidad || ''; row.querySelector('[data-prop="PrecioUnitario"]').value = option.dataset.precio || ''; }
    calcular();
  });
  rows?.addEventListener('click', e => { if (e.target.matches('.remove-row') && rows.children.length > 1) { e.target.closest('tr').remove(); calcular(); } });
  document.getElementById('agregar-detalle')?.addEventListener('click', () => { if (rows.children.length >= 100) return; const row = rows.children[0].cloneNode(true); row.querySelector('select').value = ''; row.querySelector('[data-prop="Cantidad"]').value = '1'; row.querySelector('[data-prop="PrecioUnitario"]').value = ''; row.querySelector('[data-prop="Unidad"]').value = ''; rows.append(row); calcular(); row.querySelector('select').focus(); });
  calcular();
  const costo = document.getElementById('CostoReferencia'), precio = document.getElementById('PrecioVenta'), margen = document.getElementById('margen-preview');
  function actualizarMargen() { if (!costo || !precio || !margen) return; const diferencia = Number(precio.value) - Number(costo.value); margen.textContent = `Bs ${diferencia.toFixed(2)} por unidad`; margen.style.color = diferencia <= 0 ? '#a83a38' : '#285c49'; }
  costo?.addEventListener('input', actualizarMargen); precio?.addEventListener('input', actualizarMargen); actualizarMargen();
  document.querySelectorAll('form[method="post"]').forEach(form => form.addEventListener('submit', () => { const button = form.querySelector('button[type="submit"], button:not([type])'); if (button && form.checkValidity()) { setTimeout(() => { button.disabled = true; button.textContent = 'Procesando…'; }, 0); } }));
})();
// Sprint 2 · editores de filas genéricos (previsiones y resultados de producción) y neto del pesaje.
(() => {
  function renumerar(cuerpo) {
    [...cuerpo.children].forEach((fila, i) => {
      fila.querySelectorAll('[data-prop]').forEach(c => c.name = `${cuerpo.dataset.filas}[${i}].${c.dataset.prop}`);
      const quitar = fila.querySelector('.remove-row'); if (quitar) quitar.disabled = cuerpo.children.length === 1;
    });
  }
  document.querySelectorAll('[data-filas]').forEach(cuerpo => {
    cuerpo.addEventListener('click', e => { if (e.target.matches('.remove-row') && cuerpo.children.length > 1) { e.target.closest('tr').remove(); renumerar(cuerpo); } });
    cuerpo.addEventListener('change', e => {
      const opcion = e.target.matches('select') ? e.target.selectedOptions[0] : null;
      const unidad = e.target.closest('tr')?.querySelector('[data-prop="Unidad"]');
      if (opcion && unidad && opcion.dataset.unidad) unidad.value = opcion.dataset.unidad;
    });
    renumerar(cuerpo);
  });
  document.querySelectorAll('[data-agregar-fila]').forEach(boton => boton.addEventListener('click', () => {
    const cuerpo = document.getElementById(boton.dataset.agregarFila);
    if (!cuerpo || cuerpo.children.length >= 100) return;
    const fila = cuerpo.children[0].cloneNode(true);
    fila.querySelectorAll('input:not([readonly]), select').forEach(c => { if (c.tagName === 'SELECT') c.value = ''; else if (c.type !== 'datetime-local' && c.type !== 'number') c.value = ''; });
    cuerpo.append(fila); renumerar(cuerpo); fila.querySelector('select, input')?.focus();
  }));
  const lote = document.getElementById('lote-pesaje'), unidad = document.getElementById('unidad-pesaje');
  const bruto = document.getElementById('peso-bruto'), tara = document.getElementById('peso-tara'), neto = document.getElementById('peso-neto');
  function actualizarNeto() {
    if (!bruto || !tara || !neto) return;
    if (bruto.value === '' || tara.value === '') { neto.textContent = 'Completa bruto y tara'; return; }
    const valor = Number(bruto.value) - Number(tara.value);
    neto.textContent = `${valor.toFixed(3)} ${unidad?.value || ''}`; neto.style.color = valor <= 0 ? '#a83a38' : '#285c49';
  }
  lote?.addEventListener('change', () => { const u = lote.selectedOptions[0]?.dataset.unidad; if (u && unidad) unidad.value = u; actualizarNeto(); });
  if (lote && unidad && lote.selectedOptions[0]?.dataset.unidad) unidad.value = lote.selectedOptions[0].dataset.unidad;
  bruto?.addEventListener('input', actualizarNeto); tara?.addEventListener('input', actualizarNeto); actualizarNeto();
  const tipo = document.getElementById('tipo-orden'), campoAnimal = document.getElementById('campo-animal');
  function mostrarAnimal() { if (tipo && campoAnimal) campoAnimal.hidden = tipo.value !== 'FAENEO'; }
  tipo?.addEventListener('change', mostrarAnimal); mostrarAnimal();
})();
