// ═══════════════════════════════════════════════
// AutoRepair ERP – Main JavaScript
// ═══════════════════════════════════════════════

// ── CAR TRANSITION ──────────────────────────────
function carTransition(url, label) {
    const overlay = document.getElementById('car-overlay');
    const destLabel = overlay.querySelector('.dest-label');
    if (destLabel) destLabel.textContent = label || '';
    overlay.classList.add('show');
    setTimeout(() => { window.location.href = url; }, 900);
}

// ── LIVE CLOCK ──────────────────────────────────
function updateClock() {
    const el = document.getElementById('live-clock');
    if (!el) return;
    const now = new Date();
    const h = String(now.getHours()).padStart(2, '0');
    const m = String(now.getMinutes()).padStart(2, '0');
    const s = String(now.getSeconds()).padStart(2, '0');
    const ampm = now.getHours() >= 12 ? 'PM' : 'AM';
    const hh = String(now.getHours() % 12 || 12).padStart(2, '0');
    el.textContent = `${hh}:${m}:${s} ${ampm}`;
}
setInterval(updateClock, 1000);
updateClock();

// ── STAT COUNTER ANIMATION ──────────────────────
function animateCount(el, target, prefix = '', suffix = '') {
    let current = 0;
    const isDecimal = target % 1 !== 0;
    const increment = target / 40;
    const timer = setInterval(() => {
        current += increment;
        if (current >= target) { current = target; clearInterval(timer); }
        el.textContent = prefix + (isDecimal ? current.toFixed(1) : Math.floor(current).toLocaleString()) + suffix;
    }, 30);
}

document.addEventListener('DOMContentLoaded', () => {
    // Animate stat values
    document.querySelectorAll('[data-count]').forEach(el => {
        const val = parseFloat(el.dataset.count);
        const prefix = el.dataset.prefix || '';
        const suffix = el.dataset.suffix || '';
        animateCount(el, val, prefix, suffix);
    });

    // Progress bars animate
    document.querySelectorAll('.progress-fill[data-width]').forEach(el => {
        el.style.width = '0%';
        setTimeout(() => { el.style.width = el.dataset.width; }, 300);
    });

    // Revenue chart bars animate
    document.querySelectorAll('.cbar[data-height]').forEach(el => {
        el.style.height = '5%';
        setTimeout(() => { el.style.height = el.dataset.height; }, 200);
    });

    // Inventory image slideshow
    startInventorySlideshow();
});

// ── INVENTORY IMAGE SLIDESHOW ────────────────────
const heroImages = [
    'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=200&q=80',
    'https://images.unsplash.com/photo-1600861194942-f883de0dfe96?w=200&q=80',
    'https://images.unsplash.com/photo-1609261952039-a4e1de1e6264?w=200&q=80',
    'https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=200&q=80',
    'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=200&q=80',
    'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=200&q=80',
    'https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?w=200&q=80',
    'https://images.unsplash.com/photo-1601362840469-51e4d8d58785?w=200&q=80',
];
let slideIdx = 0;

function startInventorySlideshow() {
    const imgs = document.querySelectorAll('.fimg img');
    if (!imgs.length) return;
    setInterval(() => {
        imgs.forEach((img, i) => {
            img.style.opacity = '0';
            setTimeout(() => {
                img.src = heroImages[(slideIdx + i) % heroImages.length];
                img.style.opacity = '1';
            }, 300);
        });
        slideIdx = (slideIdx + 1) % heroImages.length;
    }, 3000);
}

// ── TABS ─────────────────────────────────────────
function switchTab(el, targetId) {
    const parent = el.closest('.tabs');
    if (parent) parent.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    el.classList.add('active');
    const panel = document.getElementById(targetId);
    if (panel) {
        document.querySelectorAll('.tab-panel').forEach(p => p.style.display = 'none');
        panel.style.display = 'block';
    }
}

// ── CATEGORY PILLS ────────────────────────────────
function setCat(el, cat) {
    document.querySelectorAll('.cat-pill').forEach(p => p.classList.remove('active'));
    el.classList.add('active');
    filterParts(cat);
}

// ── PARTS FILTER ─────────────────────────────────
function filterParts(cat) {
    document.querySelectorAll('.part-card').forEach(card => {
        const cardCat = card.dataset.cat || '';
        card.style.display = (!cat || cardCat === cat) ? 'block' : 'none';
    });
}

// ── VIEW TOGGLE (Grid/List) ───────────────────────
function setView(view) {
    document.getElementById('grid-view').style.display = view === 'grid' ? '' : 'none';
    document.getElementById('list-view').style.display = view === 'list' ? '' : 'none';
    document.getElementById('vt-grid').classList.toggle('active', view === 'grid');
    document.getElementById('vt-list').classList.toggle('active', view === 'list');
}

// ── MODAL ─────────────────────────────────────────
function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

// ── TOAST ─────────────────────────────────────────
function showToast(msg, type = 'success') {
    const t = document.getElementById('toast');
    if (!t) return;
    t.textContent = msg;
    t.className = `toast ${type} show`;
    setTimeout(() => t.classList.remove('show'), 3000);
}

// ── JOB ORDER — DYNAMIC LINE ITEMS ───────────────
let partRowCount = 0;
let labourRowCount = 0;

function addPartRow() {
    partRowCount++;
    const tbody = document.getElementById('parts-tbody');
    if (!tbody) return;
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><select class="form-control" style="font-size:12px;padding:6px 10px" onchange="fillPartPrice(this)">
            <option value="">Select Part</option>
            <option value="450" data-sku="ENG-001">Engine Oil Filter — Stock: 1</option>
            <option value="2200" data-sku="BRK-001">Brake Pads Toyota — Stock: 2</option>
            <option value="380" data-sku="ELE-003">Spark Plugs NGK — Stock: 4</option>
            <option value="650" data-sku="ENG-004">Air Filter — Stock: 24</option>
            <option value="850" data-sku="OIL-001">Engine Oil 5W-30 — Stock: 48</option>
            <option value="4500" data-sku="SUS-001">Shock Absorber — Stock: 8</option>
            <option value="8500" data-sku="ELE-008">Battery 60Ah — Stock: 6</option>
        </select></td>
        <td><input type="number" class="form-control qty-input" value="1" min="1" style="width:70px;padding:6px 10px;font-size:12px" oninput="calcRowTotal(this)"></td>
        <td><input type="number" class="form-control price-input" value="0" style="width:100px;padding:6px 10px;font-size:12px" oninput="calcRowTotal(this)"></td>
        <td class="td-gold line-total" style="font-weight:700;font-family:'Syne',sans-serif">PKR 0</td>
        <td><button class="btn-danger btn-sm" onclick="this.closest('tr').remove();calcGrandTotal()" style="padding:5px 10px;font-size:12px">✕</button></td>`;
    tbody.appendChild(tr);
}

function addLabourRow() {
    labourRowCount++;
    const tbody = document.getElementById('labour-tbody');
    if (!tbody) return;
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><select class="form-control" style="font-size:12px;padding:6px 10px" onchange="fillLabourRate(this)">
            <option value="">Select Service</option>
            <option value="2500" data-hrs="0.5">Oil Change</option>
            <option value="5000" data-hrs="1.5">Brake Pad Replacement</option>
            <option value="8000" data-hrs="2.5">Engine Tune-up</option>
            <option value="4500" data-hrs="1">AC Service</option>
            <option value="15000" data-hrs="4">Full Service</option>
            <option value="custom" data-hrs="1">Custom Labour</option>
        </select></td>
        <td><input type="number" class="form-control hrs-input" value="1" min="0.5" step="0.5" style="width:80px;padding:6px 10px;font-size:12px" oninput="calcLabourTotal(this)"></td>
        <td><input type="number" class="form-control rate-input" value="1200" style="width:100px;padding:6px 10px;font-size:12px" oninput="calcLabourTotal(this)"></td>
        <td class="td-gold labour-total" style="font-weight:700;font-family:'Syne',sans-serif">PKR 1,200</td>
        <td><button class="btn-danger btn-sm" onclick="this.closest('tr').remove();calcGrandTotal()" style="padding:5px 10px;font-size:12px">✕</button></td>`;
    tbody.appendChild(tr);
    calcGrandTotal();
}

function fillPartPrice(sel) {
    const row = sel.closest('tr');
    const price = parseFloat(sel.value) || 0;
    row.querySelector('.price-input').value = price;
    calcRowTotal(sel);
}

function fillLabourRate(sel) {
    const row = sel.closest('tr');
    const opt = sel.selectedOptions[0];
    const hrs = parseFloat(opt.dataset.hrs) || 1;
    row.querySelector('.hrs-input').value = hrs;
    calcLabourTotal(sel);
}

function calcRowTotal(input) {
    const row = input.closest('tr');
    const qty = parseFloat(row.querySelector('.qty-input').value) || 0;
    const price = parseFloat(row.querySelector('.price-input').value) || 0;
    row.querySelector('.line-total').textContent = 'PKR ' + (qty * price).toLocaleString();
    calcGrandTotal();
}

function calcLabourTotal(input) {
    const row = input.closest('tr');
    const hrs = parseFloat(row.querySelector('.hrs-input').value) || 0;
    const rate = parseFloat(row.querySelector('.rate-input').value) || 0;
    row.querySelector('.labour-total').textContent = 'PKR ' + (hrs * rate).toLocaleString();
    calcGrandTotal();
}

function calcGrandTotal() {
    let partsTot = 0, labourTot = 0;
    document.querySelectorAll('.line-total').forEach(el => {
        partsTot += parseFloat(el.textContent.replace(/[^0-9.]/g, '')) || 0;
    });
    document.querySelectorAll('.labour-total').forEach(el => {
        labourTot += parseFloat(el.textContent.replace(/[^0-9.]/g, '')) || 0;
    });
    const disc = parseFloat(document.getElementById('discount-input')?.value) || 0;
    const grand = partsTot + labourTot - disc;
    const pEl = document.getElementById('parts-subtotal');
    const lEl = document.getElementById('labour-subtotal');
    const gEl = document.getElementById('grand-total');
    if (pEl) pEl.textContent = 'PKR ' + partsTot.toLocaleString();
    if (lEl) lEl.textContent = 'PKR ' + labourTot.toLocaleString();
    if (gEl) gEl.textContent = 'PKR ' + grand.toLocaleString();
}

// ── ATTEND MARK ───────────────────────────────────
let selectedStatus = 'Present';
function selectStatus(btn, status) {
    document.querySelectorAll('.status-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    selectedStatus = status;
}

function markAttendance() {
    const btn = document.getElementById('mark-att-btn');
    const now = new Date();
    const timeStr = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
    if (btn) {
        btn.disabled = true;
        btn.textContent = `✅ Marked at ${timeStr}`;
        btn.style.background = 'var(--green)';
    }
    const successEl = document.getElementById('att-success');
    if (successEl) {
        successEl.style.display = 'flex';
        successEl.querySelector('span').textContent = `✅ Attendance marked! Check-in: ${timeStr} | Status: ${selectedStatus}`;
    }
    showToast(`✅ Attendance marked — ${selectedStatus} at ${timeStr}`, 'success');
}

// ── RECEIVE STOCK — LIVE CALC ─────────────────────
function updateNewStock() {
    const qty = parseInt(document.getElementById('qty-received')?.value) || 0;
    const current = parseInt(document.getElementById('current-stock-val')?.textContent) || 2;
    const el = document.getElementById('new-stock-val');
    if (el) el.textContent = (current + qty) + ' units';
}

// ── SEARCH FILTER ─────────────────────────────────
function searchTable(inputId, tableId) {
    const q = document.getElementById(inputId).value.toLowerCase();
    document.querySelectorAll(`#${tableId} tbody tr`).forEach(row => {
        row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
    });
}


// ══════════════════════════════════════════════════════
//  COLOUR THEME SYSTEM  (per-role, independent)
// ══════════════════════════════════════════════════════
var THEMES = ['blue','green','purple','red'];

// Detect current role from a hidden meta tag written by the layout
function getCurrentRole() {
  var meta = document.getElementById('current-user-role');
  return meta ? meta.value : 'Owner';
}

function getThemeKey() {
  return 'erp_theme_' + getCurrentRole();
}

function setTheme(name) {
  if (!THEMES.includes(name)) name = 'blue';
  document.documentElement.setAttribute('data-theme', name);
  localStorage.setItem(getThemeKey(), name);
  // Update active dot
  THEMES.forEach(function(t) {
    var dot = document.getElementById('td-' + t);
    if (dot) dot.classList.toggle('active', t === name);
  });
}

function loadSavedTheme() {
  var saved = localStorage.getItem(getThemeKey()) || 'blue';
  setTheme(saved);
}

// ══════════════════════════════════════════════════════
//  MOBILE MENU
// ══════════════════════════════════════════════════════
function toggleMobileMenu() {
  var sidebar  = document.querySelector('.sidebar');
  var overlay  = document.getElementById('sidebar-overlay');
  if (!sidebar) return;
  var isOpen = sidebar.classList.contains('drawer-open');
  if (isOpen) {
    closeMobileMenu();
  } else {
    sidebar.classList.add('drawer-open');
    if (overlay) overlay.classList.add('visible');
    document.body.style.overflow = 'hidden';
  }
}
function closeMobileMenu() {
  var sidebar = document.querySelector('.sidebar');
  var overlay = document.getElementById('sidebar-overlay');
  if (sidebar) sidebar.classList.remove('drawer-open');
  if (overlay) overlay.classList.remove('visible');
  document.body.style.overflow = '';
}

// ══════════════════════════════════════════════════════
//  STATS SCROLL HINT (fade right edge until first scroll)
// ══════════════════════════════════════════════════════
function applyStatsScrollHint() {
  if (window.innerWidth > 768) return;
  document.querySelectorAll('.stats-grid,.stats-5,.stats-4,.stats-3').forEach(function(g) {
    if (g.scrollWidth > g.clientWidth + 8) {
      g.style.webkitMaskImage = 'linear-gradient(to right,black 80%,transparent 100%)';
      g.style.maskImage        = 'linear-gradient(to right,black 80%,transparent 100%)';
      g.addEventListener('scroll', function clear() {
        g.style.webkitMaskImage = 'none';
        g.style.maskImage        = 'none';
        g.removeEventListener('scroll', clear);
      });
    }
  });
}

// ══════════════════════════════════════════════════════
//  DOM READY — wire everything up
// ══════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', function () {

  // 1. Apply saved theme
  loadSavedTheme();

  // 2. Sync avatar text to mobile topbar
  var sidebarAv = document.querySelector('.user-avatar');
  var mobAv     = document.getElementById('mob-avatar');
  if (sidebarAv && mobAv) mobAv.textContent = sidebarAv.textContent.trim();

  // 3. Close drawer when any nav item tapped on mobile
  document.querySelectorAll('.nav-item').forEach(function (el) {
    el.addEventListener('click', function () {
      if (window.innerWidth <= 768) closeMobileMenu();
    });
  });

  // 4. Close drawer on Escape key
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeMobileMenu();
  });

  // 5. Stats scroll hint
  applyStatsScrollHint();

  // 6. Re-apply hint on orientation change / resize
  window.addEventListener('resize', function () {
    applyStatsScrollHint();
  });
});
