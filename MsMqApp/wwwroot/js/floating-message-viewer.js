let topZ = 1060;
const bindings = new WeakMap();
export function attach(win) {
    const controller = new AbortController();
    const options = { signal: controller.signal };
    const header = win.querySelector('header');
    const raise = () => { win.style.zIndex = ++topZ; };
    const clamp = () => {
        const r = win.getBoundingClientRect();
        win.style.left = `${Math.max(8, Math.min(r.left, innerWidth - r.width - 8))}px`;
        win.style.top = `${Math.max(8, Math.min(r.top, innerHeight - r.height - 8))}px`;
    };
    let drag;
    win.addEventListener('pointerdown', raise, options);
    win.addEventListener('focusin', raise, options);
    header.addEventListener('pointerdown', e => {
        if (e.target.closest('button') || win.classList.contains('maximized') || e.button !== 0) return;
        const r = win.getBoundingClientRect();
        drag = { x: e.clientX - r.left, y: e.clientY - r.top };
        header.setPointerCapture(e.pointerId);
        e.preventDefault();
    }, options);
    header.addEventListener('pointermove', e => {
        if (!drag) return;
        win.style.left = `${e.clientX - drag.x}px`;
        win.style.top = `${e.clientY - drag.y}px`;
        clamp();
    }, options);
    header.addEventListener('lostpointercapture', () => { drag = null; }, options);
    header.addEventListener('pointerup', e => { drag = null; if (header.hasPointerCapture(e.pointerId)) header.releasePointerCapture(e.pointerId); }, options);
    const maximize = () => { win.classList.toggle('maximized'); clamp(); };
    win.querySelector('[data-maximize]').addEventListener('click', maximize, options);
    header.addEventListener('dblclick', e => { if (!e.target.closest('button')) maximize(); }, options);
    win.addEventListener('keydown', e => { if (e.key === 'Escape') { e.stopPropagation(); win.querySelector('[aria-label="Close viewer"]').click(); } }, options);
    window.addEventListener('resize', clamp, options);
    bindings.set(win, controller);
    clamp(); raise();
    win.querySelector('select')?.focus();
}
export function detach(win) { bindings.get(win)?.abort(); bindings.delete(win); }
