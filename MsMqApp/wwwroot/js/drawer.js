// Drawer functionality for navigation menu

window.toggleDrawer = function (isOpen) {
    console.log('toggleDrawer called with:', isOpen);
    
    const drawer = document.getElementById('sidebar-drawer');
    const overlay = document.querySelector('.drawer-overlay');
    const hamburger = document.querySelector('.hamburger-btn');
    
    console.log('Elements found:', { 
        drawer: !!drawer, 
        overlay: !!overlay, 
        hamburger: !!hamburger 
    });
    
    if (!drawer || !overlay || !hamburger) {
        console.warn('Drawer elements not found', {
            drawer: !!drawer,
            overlay: !!overlay, 
            hamburger: !!hamburger
        });
        return;
    }

    if (isOpen) {
        console.log('Opening drawer');
        // Open drawer
        drawer.classList.add('open');
        overlay.classList.add('active');
        hamburger.classList.add('active');
        document.body.classList.add('drawer-open');
        
        // Prevent body scroll when drawer is open
        document.body.style.overflow = 'hidden';
    } else {
        console.log('Closing drawer');
        // Close drawer
        drawer.classList.remove('open');
        overlay.classList.remove('active');
        hamburger.classList.remove('active');
        document.body.classList.remove('drawer-open');
        
        // Restore body scroll
        document.body.style.overflow = '';
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Drawer JS loaded');
    
    // Test function availability
    if (typeof window.toggleDrawer === 'function') {
        console.log('toggleDrawer function is available');
    }
});

// Close drawer when clicking outside
document.addEventListener('click', function (event) {
    const drawer = document.getElementById('sidebar-drawer');
    const hamburger = document.querySelector('.hamburger-btn');
    
    if (!drawer || !hamburger) return;
    
    // If drawer is open and click is outside drawer and hamburger
    if (drawer.classList.contains('open') && 
        !drawer.contains(event.target) && 
        !hamburger.contains(event.target)) {
        window.toggleDrawer(false);
    }
});

// Handle escape key to close drawer
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        const drawer = document.getElementById('sidebar-drawer');
        if (drawer && drawer.classList.contains('open')) {
            window.toggleDrawer(false);
        }
    }
});

// Handle window resize
window.addEventListener('resize', function () {
    const drawer = document.getElementById('sidebar-drawer');
    if (drawer && window.innerWidth >= 1200) {
        // Auto-close drawer on large screens
        window.toggleDrawer(false);
    }
});