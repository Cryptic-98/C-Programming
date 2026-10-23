(function () {
    const wishlistButtons = document.querySelectorAll('[data-wishlist-toggle]');
    wishlistButtons.forEach(button => {
        button.addEventListener('click', async (event) => {
            event.preventDefault();

            const propertyId = button.dataset.propertyId;
            const form = button.querySelector('form') || button.closest('form');
            if (!form || !propertyId) return;

            const formData = new FormData(form);
            const token = formData.get('__RequestVerificationToken');

            const icon = button.querySelector('.wishlist-icon');
            const wasSaved = button.classList.contains('is-saved');
            button.classList.toggle('is-saved', !wasSaved);
            if (icon) {
                icon.classList.toggle('bi-heart', wasSaved);
                icon.classList.toggle('bi-heart-fill', !wasSaved);
            }

            try {
                const response = await fetch(`/Wishlist/Toggle/${propertyId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: formData
                });

                if (response.status === 401) {
                    window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                    return;
                }

                if (!response.ok) {
                    button.classList.toggle('is-saved', wasSaved);
                    if (icon) {
                        icon.classList.toggle('bi-heart', !wasSaved);
                        icon.classList.toggle('bi-heart-fill', wasSaved);
                    }
                }
            } catch (error) {
                button.classList.toggle('is-saved', wasSaved);
                if (icon) {
                    icon.classList.toggle('bi-heart', !wasSaved);
                    icon.classList.toggle('bi-heart-fill', wasSaved);
                }
            }
        });
    });

    const hideToasts = () => {
        document.querySelectorAll('.toast').forEach((toast, index) => {
            setTimeout(() => {
                toast.style.opacity = '0';
                toast.style.transition = 'opacity .3s ease';
                setTimeout(() => toast.remove(), 300);
            }, 3000 + index * 400);
        });
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', hideToasts);
    } else {
        hideToasts();
    }

    const fileInputs = document.querySelectorAll('[data-file-preview]');
    fileInputs.forEach(input => {
        input.addEventListener('change', () => {
            const preview = input.dataset.filePreview;
            const container = document.getElementById(preview);
            if (!container) return;
            container.innerHTML = '';
            Array.from(input.files || []).forEach((file, index) => {
                const reader = new FileReader();
                reader.onload = (event) => {
                    const item = document.createElement('div');
                    item.className = 'photo-thumb';
                    item.innerHTML = `<img src="${event.target.result}" alt="Property photo ${index + 1}" /><span class="photo-thumb-label">${index === 0 ? 'Cover photo' : 'Photo ' + (index + 1)}</span>`;
                    container.appendChild(item);
                };
                reader.readAsDataURL(file);
            });
        });
    });
})();
