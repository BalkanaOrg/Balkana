// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Mobile menu toggle functionality
(function() {
    'use strict';
    
    let menuBtn, navbar, overlay, body;
    let isInitialized = false;
    
    function init() {
        menuBtn = document.getElementById('mobileMenuBtn');
        navbar = document.getElementById('topNavbar');
        overlay = document.getElementById('mobileOverlay');
        body = document.body;
        
        if (!menuBtn || !navbar || !overlay) {
            return false;
        }
        
        if (isInitialized) {
            return true;
        }
        
        function toggleMenu(e) {
            if (e) {
                e.preventDefault();
                e.stopPropagation();
            }
            
            const isOpen = navbar.classList.contains('menu-open');
            
            if (isOpen) {
                closeMenu();
            } else {
                openMenu();
            }
        }
        
        function openMenu() {
            navbar.classList.add('menu-open');
            menuBtn.classList.add('active');
            menuBtn.setAttribute('aria-expanded', 'true');
            overlay.classList.add('active');
            body.classList.add('menu-open');
        }
        
        function closeMenu() {
            navbar.classList.remove('menu-open');
            menuBtn.classList.remove('active');
            menuBtn.setAttribute('aria-expanded', 'false');
            overlay.classList.remove('active');
            body.classList.remove('menu-open');
        }
        
        // Simple click handler - works on both mobile and desktop
        menuBtn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            toggleMenu(e);
        }, false);
        
        overlay.addEventListener('click', closeMenu);
        overlay.addEventListener('touchstart', closeMenu);
        
        // Close menu when clicking on nav links (including mobile-only links)
        const navLinks = navbar.querySelectorAll('.nav-link, .mobile-only-link');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                if (window.innerWidth <= 768) {
                    setTimeout(closeMenu, 300);
                }
            });
        });
        
        // Close menu on window resize if desktop and update button visibility
        function updateButtonVisibility() {
            if (window.innerWidth <= 768) {
                menuBtn.style.display = 'flex';
            } else {
                menuBtn.style.display = 'none';
                closeMenu();
            }
        }
        
        window.addEventListener('resize', function() {
            updateButtonVisibility();
        });
        
        // Initial check
        updateButtonVisibility();
        
        // Close menu on escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && navbar.classList.contains('menu-open')) {
                closeMenu();
            }
        });
        
        // Mobile dropdown toggle (for touch devices)
        const dropdownBtns = navbar.querySelectorAll('.dropdown-btn');
        dropdownBtns.forEach(btn => {
            btn.addEventListener('click', function(e) {
                // Only handle click on mobile, let hover work on desktop
                if (window.innerWidth <= 768) {
                    e.preventDefault();
                    e.stopPropagation();
                    const dropdown = this.closest('.dropdown');
                    dropdown.classList.toggle('is-open');
                }
            });
        });
        
        // Close dropdowns when switching to desktop
        window.addEventListener('resize', function() {
            if (window.innerWidth > 768) {
                navbar.querySelectorAll('.dropdown').forEach(dropdown => {
                    dropdown.classList.remove('is-open');
                });
            }
        });
        
        isInitialized = true;
        return true;
    }
    
    // Try to initialize immediately
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            if (!init()) {
                // Retry after a short delay
                setTimeout(function() {
                    init();
                }, 100);
            }
        });
    } else {
        if (!init()) {
            // Retry after a short delay
            setTimeout(function() {
                init();
            }, 100);
        }
    }
    
    // Also try on window load as a fallback
    window.addEventListener('load', function() {
        if (!isInitialized) {
            init();
        }
    });
})();
