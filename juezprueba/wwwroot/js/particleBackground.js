// particleBackground.js
document.addEventListener('DOMContentLoaded', function () {
    // Solo aplicar si no hay ya un canvas de partículas
    if (document.getElementById('particle-canvas')) return;

    const canvas = document.createElement('canvas');
    canvas.id = 'particle-canvas';
    canvas.style.position = 'fixed';
    canvas.style.top = '0';
    canvas.style.left = '0';
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    canvas.style.zIndex = '-1';
    canvas.style.opacity = '0.5';
    canvas.style.pointerEvents = 'none';
    canvas.style.transition = 'opacity 1.2s ease';
    document.body.appendChild(canvas);

    const ctx = canvas.getContext('2d');
    resizeCanvas();

    // Configuración de partículas
    const particles = [];
    const particleCount = Math.min(Math.floor(window.innerWidth / 10), 100);
    const colors = {
        light: '#a8d0e6', // Azul claro para tema claro
        dark: '#4b79a7'   // Azul oscuro para tema oscuro
    };

    // Crear partículas
    for (let i = 0; i < particleCount; i++) {
        particles.push(createParticle());
    }

    function createParticle() {
        const isDarkTheme = document.documentElement.getAttribute('data-bs-theme') === 'dark';
        const color = isDarkTheme ? colors.dark : colors.light;

        return {
            x: Math.random() * canvas.width,
            y: Math.random() * canvas.height,
            size: Math.random() * 3 + 1,
            speedX: Math.random() * 2 - 1,
            speedY: Math.random() * 2 - 1,
            color: color
        };
    }

    function animate() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Actualizar y dibujar partículas
        for (let i = 0; i < particles.length; i++) {
            const p = particles[i];

            // Actualizar posición
            p.x += p.speedX;
            p.y += p.speedY;

            // Rebotar en los bordes
            if (p.x < 0 || p.x > canvas.width) p.speedX *= -1;
            if (p.y < 0 || p.y > canvas.height) p.speedY *= -1;

            // Dibujar partícula
            ctx.fillStyle = p.color;
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
            ctx.fill();
        }

        requestAnimationFrame(animate);
    }

    function resizeCanvas() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    }

    // Observador para cambios de tema
    const observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            if (mutation.attributeName === 'data-bs-theme') {
                const isDarkTheme = document.documentElement.getAttribute('data-bs-theme') === 'dark';
                particles.forEach(p => {
                    p.color = isDarkTheme ? colors.dark : colors.light;
                });
            }
        });
    });

    observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['data-bs-theme']
    });

    // Manejar redimensionamiento
    window.addEventListener('resize', function () {
        resizeCanvas();
    });

    // Iniciar animación
    animate();
});