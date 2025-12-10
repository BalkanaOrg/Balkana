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

// Global Search Autocomplete Functionality
(function() {
    'use strict';
    
    let searchInput, searchResults, searchTimeout, activeIndex = -1;
    let scrollHandler, resizeHandler;
    
    function initSearch() {
        searchInput = document.getElementById('globalSearchInput');
        searchResults = document.getElementById('searchResults');
        
        if (!searchInput || !searchResults) {
            return false;
        }
        
        // Debounced search on input
        searchInput.addEventListener('input', function(e) {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();
            
            if (query.length < 2) {
                hideResults();
                return;
            }
            
            searchTimeout = setTimeout(() => {
                performSearch(query);
            }, 300);
        });
        
        // Reposition results when input is focused
        searchInput.addEventListener('focus', function() {
            if (searchResults.style.display === 'block') {
                positionSearchResults();
            }
        });
        
        // Handle keyboard navigation
        searchInput.addEventListener('keydown', function(e) {
            if (!searchResults.style.display || searchResults.style.display === 'none') {
                return;
            }
            
            const items = searchResults.querySelectorAll('.search-results-item');
            
            switch(e.key) {
                case 'ArrowDown':
                    e.preventDefault();
                    activeIndex = Math.min(activeIndex + 1, items.length - 1);
                    updateActiveItem(items);
                    break;
                case 'ArrowUp':
                    e.preventDefault();
                    activeIndex = Math.max(activeIndex - 1, -1);
                    updateActiveItem(items);
                    break;
                case 'Enter':
                    e.preventDefault();
                    if (activeIndex >= 0 && items[activeIndex]) {
                        items[activeIndex].click();
                    }
                    break;
                case 'Escape':
                    hideResults();
                    searchInput.blur();
                    break;
            }
        });
        
        // Hide results when clicking outside
        document.addEventListener('click', function(e) {
            if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
                hideResults();
            }
        });
        
        return true;
    }
    
    function updateActiveItem(items) {
        items.forEach((item, index) => {
            if (index === activeIndex) {
                item.style.background = 'rgba(255, 255, 255, 0.15)';
                item.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            } else {
                item.style.background = '';
            }
        });
    }
    
    async function performSearch(query) {
        try {
            showLoading();
            
            const response = await fetch(`/api/search/autocomplete?query=${encodeURIComponent(query)}&limit=5`);
            
            if (!response.ok) {
                throw new Error('Search failed');
            }
            
            const data = await response.json();
            displayResults(data);
        } catch (error) {
            console.error('Search error:', error);
            showError();
        }
    }
    
    function positionSearchResults() {
        if (!searchInput || !searchResults) return;
        
        const inputRect = searchInput.getBoundingClientRect();
        
        // For fixed positioning, use viewport coordinates directly (getBoundingClientRect already gives viewport-relative)
        searchResults.style.top = (inputRect.bottom + 8) + 'px';
        searchResults.style.left = inputRect.left + 'px';
        
        // Adjust if results would go off right edge of screen
        const maxWidth = Math.min(600, window.innerWidth - inputRect.left - 20);
        searchResults.style.maxWidth = maxWidth + 'px';
    }
    
    function attachPositionHandlers() {
        // Remove existing handlers if any
        if (scrollHandler) {
            window.removeEventListener('scroll', scrollHandler);
        }
        if (resizeHandler) {
            window.removeEventListener('resize', resizeHandler);
        }
        
        // Create new handlers
        scrollHandler = function() {
            positionSearchResults();
        };
        resizeHandler = function() {
            positionSearchResults();
        };
        
        // Attach handlers
        window.addEventListener('scroll', scrollHandler, { passive: true });
        window.addEventListener('resize', resizeHandler);
    }
    
    function detachPositionHandlers() {
        if (scrollHandler) {
            window.removeEventListener('scroll', scrollHandler);
            scrollHandler = null;
        }
        if (resizeHandler) {
            window.removeEventListener('resize', resizeHandler);
            resizeHandler = null;
        }
    }
    
    function showLoading() {
        searchResults.innerHTML = '<div class="search-results-loading">Searching...</div>';
        positionSearchResults();
        searchResults.style.display = 'block';
        activeIndex = -1;
        attachPositionHandlers();
    }
    
    function showError() {
        searchResults.innerHTML = '<div class="search-results-empty">Search unavailable. Please try again.</div>';
        positionSearchResults();
        searchResults.style.display = 'block';
        attachPositionHandlers();
    }
    
    function displayResults(data) {
        const hasResults = (data.teams && data.teams.length > 0) ||
                          (data.players && data.players.length > 0) ||
                          (data.tournaments && data.tournaments.length > 0) ||
                          (data.articles && data.articles.length > 0);
        
        if (!hasResults) {
            searchResults.innerHTML = '<div class="search-results-empty">No results found</div>';
            searchResults.style.display = 'block';
            activeIndex = -1;
            return;
        }
        
        let html = '';
        
        // Teams
        if (data.teams && data.teams.length > 0) {
            html += '<div class="search-results-category">';
            html += '<div class="search-results-category-title"><i class="fi fi-rr-users-alt"></i> Teams</div>';
            data.teams.forEach(item => {
                html += `<a href="${item.url}" class="search-results-item">
                    <div class="search-results-item-icon"><i class="fi fi-rr-users-alt"></i></div>
                    <div class="search-results-item-content">
                        <div class="search-results-item-title">${escapeHtml(item.tag)}</div>
                        <div class="search-results-item-subtitle">${escapeHtml(item.name)} • ${escapeHtml(item.game || '')}</div>
                    </div>
                </a>`;
            });
            html += '</div>';
        }
        
        // Players
        if (data.players && data.players.length > 0) {
            html += '<div class="search-results-category">';
            html += '<div class="search-results-category-title"><i class="fi fi-rr-user"></i> Players</div>';
            data.players.forEach(item => {
                const subtitle = item.fullName ? `${escapeHtml(item.fullName)}` : '';
                html += `<a href="${item.url}" class="search-results-item">
                    <div class="search-results-item-icon"><i class="fi fi-rr-user"></i></div>
                    <div class="search-results-item-content">
                        <div class="search-results-item-title">${escapeHtml(item.name)}</div>
                        ${subtitle ? `<div class="search-results-item-subtitle">${escapeHtml(subtitle)}</div>` : ''}
                    </div>
                </a>`;
            });
            html += '</div>';
        }
        
        // Tournaments
        if (data.tournaments && data.tournaments.length > 0) {
            html += '<div class="search-results-category">';
            html += '<div class="search-results-category-title"><i class="fi fi-rr-trophy"></i> Tournaments</div>';
            data.tournaments.forEach(item => {
                html += `<a href="${item.url}" class="search-results-item">
                    <div class="search-results-item-icon"><i class="fi fi-rr-trophy"></i></div>
                    <div class="search-results-item-content">
                        <div class="search-results-item-title">${escapeHtml(item.name)}</div>
                        <div class="search-results-item-subtitle">${escapeHtml(item.shortName)} • ${escapeHtml(item.game || '')}</div>
                    </div>
                </a>`;
            });
            html += '</div>';
        }
        
        // Articles
        if (data.articles && data.articles.length > 0) {
            html += '<div class="search-results-category">';
            html += '<div class="search-results-category-title"><i class="fi fi-rr-newspaper"></i> Articles</div>';
            data.articles.forEach(item => {
                html += `<a href="${item.url}" class="search-results-item">
                    <div class="search-results-item-icon"><i class="fi fi-rr-newspaper"></i></div>
                    <div class="search-results-item-content">
                        <div class="search-results-item-title">${escapeHtml(item.name)}</div>
                    </div>
                </a>`;
            });
            html += '</div>';
        }
        
        searchResults.innerHTML = html;
        positionSearchResults();
        searchResults.style.display = 'block';
        activeIndex = -1;
        attachPositionHandlers();
    }
    
    function hideResults() {
        searchResults.style.display = 'none';
        activeIndex = -1;
        detachPositionHandlers();
    }
    
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSearch);
    } else {
        initSearch();
    }
})();
