// Cart functionality
let cartOffcanvas;

// Initialize offcanvas on page load
document.addEventListener('DOMContentLoaded', function () {
    const offcanvasElement = document.getElementById('cartOffcanvas');
    cartOffcanvas = new bootstrap.Offcanvas(offcanvasElement);

    // Load cart count on page load
    updateCartBadge();
});

// Open cart and load content
function openCart() {
    loadCartContent();
    cartOffcanvas.show();
}

// Load cart content via AJAX
function loadCartContent() {
    const cartBody = document.getElementById('cart-offcanvas-body');

    // Show loading spinner
    cartBody.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Cargando...</span>
            </div>
        </div>
    `;


    fetch('/Cart/GetCartPartial')
        .then(response => response.text())
        .then(html => {
            cartBody.innerHTML = html;
            updateCartBadge();
        })
        .catch(error => {
            console.error('Error loading cart:', error);
            cartBody.innerHTML = `
                <div class="alert alert-danger">
                    Error al cargar el carrito. Por favor, intenta nuevamente.
                </div>
            `;
        });
}

// Add item to cart
function addToCart(productId, quantity) {

    const formData = new FormData();
    formData.append('ProductId', productId);
    formData.append('Quantity', quantity || 1);

    fetch('/Cart/AddToCart', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Error al agregar producto');
            }
            return response.text();
        })
        .then(html => {
            const cartBody = document.getElementById('cart-offcanvas-body');
            cartBody.innerHTML = html;
            updateCartBadge();

            // Open cart automatically after adding
            cartOffcanvas.show();

            // Show success feedback
            showToast('Producto agregado al carrito');
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('Error al agregar producto', 'danger');
        });
}

// Update cart item quantity
function updateCartQuantity(productId, quantity) {
    const formData = new FormData();
    formData.append('ProductId', productId);
    formData.append('Quantity', quantity);

    fetch('/Cart/UpdateCart', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Error al actualizar cantidad');
            }
            return response.text();
        })
        .then(html => {
            const cartBody = document.getElementById('cart-offcanvas-body');
            cartBody.innerHTML = html;
            updateCartBadge();
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('Error al actualizar cantidad', 'danger');
        });
}

// Remove item from cart
function removeFromCart(productId) {
    if (!confirm('¿Deseas eliminar este producto del carrito?')) {
        return;
    }

    const formData = new FormData();
    formData.append('productId', productId);

    fetch('/Cart/RemoveFromCart', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Error al eliminar producto');
            }
            return response.text();
        })
        .then(html => {
            const cartBody = document.getElementById('cart-offcanvas-body');
            cartBody.innerHTML = html;
            updateCartBadge();
            showToast('Producto eliminado');
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('Error al eliminar producto', 'danger');
        });
}

// Clear entire cart
function clearCart() {
    if (!confirm('¿Deseas vaciar todo el carrito?')) {
        return;
    }

    fetch('/Cart/ClearCart', {
        method: 'POST'
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Error al vaciar carrito');
            }
            return response.text();
        })
        .then(html => {
            const cartBody = document.getElementById('cart-offcanvas-body');
            cartBody.innerHTML = html;
            updateCartBadge();
            showToast('Carrito vaciado');
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('Error al vaciar carrito', 'danger');
        });
}

// Update cart badge count
function updateCartBadge() {
    fetch('/Cart/GetCartPartial')
        .then(response => response.text())
        .then(html => {
            // Parse HTML to extract item count
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const cartContent = doc.getElementById('cart-content');

            // Count items by looking for cart-item divs
            /*
                Count items by looking for cart-item divs
                Cuente los artículos buscando los divs de los artículos del carrito
            */
            const items = cartContent.querySelectorAll('.cart-item');
            const itemCount = items.length;

            const badge = document.getElementById('cart-badge');
            if (itemCount > 0) {
                badge.textContent = itemCount;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        })
        .catch(error => {
            console.error('Error updating cart badge:', error);
        });
}

// Proceed to checkout
function proceedToCheckout() {
    // Show loading state
    const cartBody = document.getElementById('cart-offcanvas-body');

    cartBody.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Procesando...</span>
            </div>
            <p class="mt-3">Preparando pago...</p>
        </div>
    `;

    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]');

    const formData = new FormData();
    if (token) {
        formData.append('__RequestVerificationToken', token.value);
    }

    // Call checkout endpoint
    fetch('/Checkout/checkout', {
        method: 'POST',
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Error al procesar checkout');
            }
            return response.text();
        })
        .then(html => {
            // Replace offcanvas content with MercadoPago wallet
            cartBody.innerHTML = html;

            // Initialize MercadoPago wallet after loading HTML
            initializeMercadoPagoWallet();
        })
        .catch(error => {
            console.error('Error:', error);
            cartBody.innerHTML = `
            <div class="text-center py-5">
                <i class="bi bi-exclamation-triangle display-1 text-danger"></i>
                <h4 class="mt-3 text-danger">Error</h4>
                <p class="text-muted">No se pudo procesar el pago. Intenta nuevamente.</p>
                <button type="button" class="btn btn-primary mt-3" onclick="loadCartContent()">
                    <i class="bi bi-arrow-left"></i> Volver al Carrito
                </button>
            </div>
        `;
        });
}

// Initialize MercadoPago wallet after dynamic content load
function initializeMercadoPagoWallet() {
    const walletContent = document.getElementById('checkout-wallet-content');

    if (!walletContent) {
        console.log('Wallet content not found');
        return;
    }

    const preferenceId = walletContent.getAttribute('data-preference-id');
    const publicKey = walletContent.getAttribute('data-public-key');

    if (!preferenceId || !publicKey) {
        console.error('Missing MercadoPago credentials');
        return;
    }

    // Load MercadoPago SDK if not already loaded
    //Cargar el SDK de MercadoPago si aún no está cargado
    if (typeof MercadoPago === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://sdk.mercadopago.com/js/v2';
        script.onload = function () {
            createMercadoPagoWallet(publicKey, preferenceId);
        };
        document.head.appendChild(script);
    } else {
        createMercadoPagoWallet(publicKey, preferenceId);
    }
}

// Create MercadoPago wallet
function createMercadoPagoWallet(publicKey, preferenceId) {
    try {
        const mp = new MercadoPago(publicKey, { locale: "es-AR" });
        const bricks = mp.bricks();

        bricks.create("wallet", "mp-wallet-container-offcanvas", {
            initialization: {
                preferenceId: preferenceId
            },
            customization: {
                texts: {
                    valueProp: 'smart_option'
                }
            }
        }).then(() => {
            console.log('MercadoPago wallet initialized successfully');
        }).catch((error) => {
            console.error('Error creating MercadoPago wallet:', error);
        });
    } catch (error) {
        console.error('Error initializing MercadoPago:', error);
    }
}

// Show toast notification
function showToast(message, type = 'success') {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    toastContainer.insertAdjacentHTML('beforeend', toastHtml);

    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 3000
    });

    toast.show();

    // Remove toast element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function () {
        toastElement.remove();
    });
}