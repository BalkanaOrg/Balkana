// Main JavaScript file for Balkan Championship

// Smooth scrolling function
function scrollToGames() {
    document.getElementById('games').scrollIntoView({
        behavior: 'smooth'
    });
}

// Navigation interaction
document.addEventListener('DOMContentLoaded', function () {
    const navItems = document.querySelectorAll('.nav-item');

    navItems.forEach(item => {
        item.addEventListener('click', function () {
            // Remove active class from all items
            navItems.forEach(nav => nav.classList.remove('active'));
            // Add active class to clicked item
            this.classList.add('active');

            // Handle navigation based on data-page attribute
            const page = this.getAttribute('data-page');
            handleNavigation(page);
        });
    });

    // Register button interactions
    document.querySelectorAll('.register-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const gameTitle = this.parentElement.querySelector('.game-title').textContent;
            showRegistrationModal(gameTitle);
        });
    });

    // Initialize animations
    initializeAnimations();
});

// Handle navigation
function handleNavigation(page) {
    switch (page) {
        case 'home':
            scrollToTop();
            break;
        case 'competitions':
        case 'fantasy':
            scrollToGames();
            break;
        case 'ranking':
            // Future: scroll to rankings section
            console.log('Rankings section - coming soon!');
            break;
        case 'schedule':
            // Future: scroll to schedule section
            console.log('Schedule section - coming soon!');
            break;
        case 'news':
            scrollToNews();
            break;
        case 'about':
            scrollToFooter();
            break;
        case 'gallery':
            console.log('Gallery section - coming soon!');
            break;
        case 'festival':
            console.log('Festival section - coming soon!');
            break;
        case 'more':
            console.log('More options - coming soon!');
            break;
        default:
            console.log(`Navigation to ${page} not implemented yet`);
    }
}

// Scroll functions
function scrollToTop() {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
}

function scrollToNews() {
    const newsSection = document.querySelector('.news-section');
    if (newsSection) {
        newsSection.scrollIntoView({
            behavior: 'smooth'
        });
    }
}

function scrollToFooter() {
    const footer = document.querySelector('.footer');
    if (footer) {
        footer.scrollIntoView({
            behavior: 'smooth'
        });
    }
}

// Registration modal
function showRegistrationModal(gameTitle) {
    // Create modal
    const modal = document.createElement('div');
    modal.className = 'registration-modal';
    modal.innerHTML = `
        <div class="modal-content">
            <div class="modal-header">
                <h3>Register for ${gameTitle}</h3>
                <button class="close-modal" onclick="closeModal()">&times;</button>
            </div>
            <div class="modal-body">
                <p>Registration for ${gameTitle} tournament will be available soon!</p>
                <p>Follow us on social media for updates and announcements.</p>
                <div class="modal-features">
                    <div class="feature">✨ Early bird registration discounts</div>
                    <div class="feature">🏆 Exclusive team benefits</div>
                    <div class="feature">📺 Live streaming opportunities</div>
                </div>
            </div>
            <div class="modal-footer">
                <button class="modal-btn secondary" onclick="closeModal()">Maybe Later</button>
                <button class="modal-btn primary" onclick="subscribeToUpdates()">Notify Me!</button>
            </div>
        </div>
    `;

    // Add modal styles
    addModalStyles();

    document.body.appendChild(modal);
    document.body.style.overflow = 'hidden';

    // Add click outside to close
    modal.addEventListener('click', function (e) {
        if (e.target === modal) {
            closeModal();
        }
    });
}

function addModalStyles() {
    if (document.querySelector('#modal-styles')) return;

    const modalStyles = `
        .registration-modal {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.8);
            backdrop-filter: blur(5px);
            display: flex;
            justify-content: center;
            align-items: center;
            z-index: 10000;
            animation: fadeIn 0.3s ease;
        }
        
        .modal-content {
            background: linear-gradient(135deg, rgba(45, 27, 105, 0.9) 0%, rgba(139, 0, 0, 0.9) 100%);
            backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.2);
            border-radius: 20px;
            padding: 30px;
            max-width: 450px;
            width: 90%;
            text-align: center;
            box-shadow: 0 20px 40px rgba(45, 27, 105, 0.3);
            transform: scale(0.9);
            animation: modalSlideIn 0.3s ease forwards;
        }
        
        .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
        }
        
        .modal-header h3 {
            color: white;
            font-size: 1.5rem;
            font-weight: 800;
        }
        
        .close-modal {
            background: none;
            border: none;
            color: white;
            font-size: 24px;
            cursor: pointer;
            padding: 0;
            width: 30px;
            height: 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 50%;
            transition: background 0.3s ease;
        }
        
        .close-modal:hover {
            background: rgba(255, 255, 255, 0.1);
        }
        
        .modal-body {
            margin-bottom: 25px;
        }
        
        .modal-body p {
            color: rgba(255, 255, 255, 0.9);
            line-height: 1.5;
            margin-bottom: 15px;
        }
        
        .modal-features {
            margin: 20px 0;
            text-align: left;
        }
        
        .feature {
            color: rgba(255, 255, 255, 0.8);
            margin: 8px 0;
            font-size: 14px;
        }
        
        .modal-footer {
            display: flex;
            gap: 15px;
            justify-content: center;
        }
        
        .modal-btn {
            border: none;
            padding: 12px 25px;
            border-radius: 25px;
            color: white;
            font-weight: 700;
            cursor: pointer;
            transition: all 0.3s ease;
            text-transform: uppercase;
            letter-spacing: 1px;
            font-size: 12px;
        }
        
        .modal-btn.primary {
            background: linear-gradient(45deg, #8b0000, #2d1b69);
        }
        
        .modal-btn.secondary {
            background: transparent;
            border: 1px solid rgba(255, 255, 255, 0.3);
        }
        
        .modal-btn:hover {
            transform: translateY(-2px);
        }
        
        .modal-btn.primary:hover {
            box-shadow: 0 8px 20px rgba(139, 0, 0, 0.4);
        }
        
        .modal-btn.secondary:hover {
            background: rgba(255, 255, 255, 0.1);
        }
        
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
        
        @keyframes modalSlideIn {
            from { transform: scale(0.9) translateY(-20px); opacity: 0; }
            to { transform: scale(1) translateY(0); opacity: 1; }
        }
    `;

    const styleElement = document.createElement('style');
    styleElement.id = 'modal-styles';
    styleElement.textContent = modalStyles;
    document.head.appendChild(styleElement);
}

function closeModal() {
    const modal = document.querySelector('.registration-modal');
    if (modal) {
        modal.style.animation = 'fadeOut 0.3s ease';
        setTimeout(() => {
            modal.remove();
            document.body.style.overflow = 'auto';
        }, 300);
    }
}

function subscribeToUpdates() {
    alert('Thank you! We\'ll notify you when registration opens. Check your email for confirmation.');
    closeModal();
}

// Parallax effect on scroll
let ticking = false;

function updateParallax() {
    const scrolled = window.pageYOffset;
    const hero = document.querySelector('.hero');
    if (hero && scrolled < window.innerHeight) {
        hero.style.transform = `translateY(${scrolled * 0.3}px)`;
    }
    ticking = false;
}

window.addEventListener('scroll', () => {
    if (!ticking) {
        requestAnimationFrame(updateParallax);
        ticking = true;
    }

    // Update active navigation based on scroll
    updateActiveNavigation();
});

// Dynamic active nav based on scroll position
function updateActiveNavigation() {
    const sections = [
        { element: document.querySelector('.hero'), nav: 'home' },
        { element: document.getElementById('games'), nav: 'competitions' },
        { element: document.querySelector('.news-section'), nav: 'news' },
        { element: document.querySelector('.footer'), nav: 'about' }
    ];

    const scrollPos = window.scrollY + 200;

    sections.forEach(section => {
        if (section.element) {
            const offsetTop = section.element.offsetTop;
            const offsetBottom = offsetTop + section.element.offsetHeight;

            if (scrollPos >= offsetTop && scrollPos < offsetBottom) {
                const navItems = document.querySelectorAll('.nav-item');
                navItems.forEach(item => {
                    item.classList.remove('active');
                    if (item.getAttribute('data-page') === section.nav) {
                        item.classList.add('active');
                    }
                });
            }
        }
    });
}

// Initialize animations
function initializeAnimations() {
    // Intersection Observer for animations
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.animation = 'slideInUp 0.6s ease forwards';
                entry.target.style.opacity = '1';
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    });

    // Observe elements for animation
    const elementsToAnimate = [
        ...document.querySelectorAll('.game-card'),
        ...document.querySelectorAll('.stat-card'),
        ...document.querySelectorAll('.footer-section')
    ];

    elementsToAnimate.forEach(element => {
        element.style.opacity = '0';
        observer.observe(element);
    });
}

// Handle social media links
document.addEventListener('click', function (e) {
    if (e.target.classList.contains('social-link')) {
        e.preventDefault();
        const socialType = e.target.textContent.trim();

        let url = '#';
        let platform = '';

        switch (socialType) {
            case '📘':
                url = 'https://facebook.com/balkanchampionship';
                platform = 'Facebook';
                break;
            case '📷':
                url = 'https://instagram.com/balkanchampionship';
                platform = 'Instagram';
                break;
            case '🐦':
                url = 'https://twitter.com/balkanchampionship';
                platform = 'Twitter';
                break;
            case '📺':
                url = 'https://youtube.com/balkanchampionship';
                platform = 'YouTube';
                break;
            case '💬':
                url = 'https://discord.gg/balkanchampionship';
                platform = 'Discord';
                break;
        }

        if (url !== '#') {
            // Show a message since these are placeholder links
            const message = `This would open our ${platform} page! \n(${url})`;
            if (confirm(message + '\n\nOpen placeholder link?')) {
                window.open(url, '_blank');
            }
        }
    }
});

// Interactive mouse effects for hero section
document.addEventListener('mousemove', function (e) {
    const hero = document.querySelector('.hero');
    if (hero && window.scrollY < window.innerHeight) {
        const x = (e.clientX / window.innerWidth) * 100;
        const y = (e.clientY / window.innerHeight) * 100;

        hero.style.background = `
            linear-gradient(rgba(0, 0, 0, 0.7), rgba(0, 0, 0, 0.5)), 
            radial-gradient(circle at ${x}% ${y}%, rgba(45, 27, 105, 0.4) 0%, transparent 50%),
            radial-gradient(circle at ${100 - x}% ${100 - y}%, rgba(139, 0, 0, 0.4) 0%, transparent 50%)
        `;
    }
});

// Loading animation
window.addEventListener('load', function () {
    document.body.classList.add('loaded');

    // Add a subtle entrance animation for the whole page
    document.body.style.opacity = '0';
    document.body.style.transition = 'opacity 0.5s ease';

    setTimeout(() => {
        document.body.style.opacity = '1';
    }, 100);
});

// Handle ESC key to close modal
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        const modal = document.querySelector('.registration-modal');
        if (modal) {
            closeModal();
        }
    }
});

// Add fadeOut animation for modal closing
const additionalStyles = `
    @keyframes fadeOut {
        from { opacity: 1; }
        to { opacity: 0; }
    }
`;

// Console welcome message
console.log('🎮 Welcome to Balkan Championship! 🏆');
console.log('Website loaded successfully with:');
console.log('✅ Interactive navigation');
console.log('✅ Smooth animations');
console.log('✅ Registration modals');
console.log('✅ Responsive design');
console.log('✅ Dark red/blue theme');

// Performance monitoring
if (window.performance) {
    window.addEventListener('load', function () {
        setTimeout(function () {
            const loadTime = window.performance.timing.loadEventEnd - window.performance.timing.navigationStart;
            console.log(`⚡ Page loaded in ${loadTime}ms`);
        }, 0);
    });
}