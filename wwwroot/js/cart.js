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
    fetch('/MercadoPago/checkout', {
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

// ============================================================
// CHECKOUT CON QR DINÁMICO (MERCADO PAGO INSTORE ORDERS)
// ============================================================
//
// Este módulo implementa el flujo de pago con QR dinámico:
// 1. Usuario hace clic en "Pagar con QR"
// 2. Se llama a /MercadoPago/PaymentQr para generar el QR
// 3. Se muestra el QR en el offcanvas
// 4. Se inicia conexión SignalR para recibir notificación de pago
// 5. Usuario escanea el QR con la app de Mercado Pago
// 6. MP envía webhook al servidor
// 7. El servidor notifica vía SignalR
// 8. El cliente redirige al usuario
//
// Dependencias:
// - qr-payment.js: Cliente SignalR (startQrPaymentNotificationClient, cleanupQrPaymentNotification)
// - signalr.js: Librería de Microsoft SignalR
// ============================================================

/**
 * Inicia el proceso de checkout con QR Dinámico.
 *
 * Flujo detallado:
 * 1. Mostrar spinner de "Generando código QR..."
 * 2. Llamar a POST /MercadoPago/PaymentQr
 * 3. El servidor crea la orden local y llama a la API de MP
 * 4. MP retorna el qr_data (string EMV)
 * 5. El servidor genera la imagen QR y la retorna como HTML
 * 6. Insertar el HTML en el offcanvas
 * 7. Inicializar SignalR manualmente (los scripts en innerHTML no se ejecutan)
 * 8. SignalR se conecta y se une al grupo de la orden
 * 9. Cuando llega el webhook, SignalR notifica y redirigimos
 *
 * IMPORTANTE sobre innerHTML y scripts:
 * Cuando se inserta HTML via innerHTML, los <script> dentro NO se ejecutan.
 * Esto es una medida de seguridad del navegador. Por eso debemos llamar
 * a startQrPaymentNotificationClient() manualmente después de insertar el HTML.
 */
function proceedToQr() {
    // Referencia al contenedor del carrito (offcanvas body)
    const cartBody = document.getElementById('cart-offcanvas-body');

    // Mostrar estado de carga mientras se genera el QR
    cartBody.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Generando código QR...</span>
            </div>
            <p class="mt-3">Generando código QR...</p>
        </div>
    `;

    // Obtener token anti-forgery para protección CSRF
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    const formData = new FormData();
    if (token) {
        formData.append('__RequestVerificationToken', token.value);
    }

    // Llamar al endpoint que genera el QR
    // POST /MercadoPago/PaymentQr
    fetch('/MercadoPago/paymentQr', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Error al generar código QR');
        }
        return response.text();  // El servidor retorna HTML
    })
    .then(html => {
        // Insertar el HTML de la vista _Qr.cshtml en el offcanvas
        cartBody.innerHTML = html;

        // ============================================================
        // INICIALIZACIÓN MANUAL DE SIGNALR
        // ============================================================
        // IMPORTANTE: Los scripts dentro de innerHTML NO se ejecutan automáticamente.
        // Esto es una medida de seguridad del navegador.
        // Por eso debemos inicializar SignalR manualmente.
        //
        // La vista _Qr.cshtml tiene un div con data-order-id que contiene
        // el ID de la orden. Extraemos ese ID y llamamos a la función
        // de qr-payment.js para iniciar la conexión SignalR.
        // ============================================================
        const qrContent = document.getElementById('checkout-qr-content');
        if (qrContent) {
            // Extraer el orderId del atributo data-order-id del HTML
            const orderId = qrContent.getAttribute('data-order-id');

            if (orderId && typeof startQrPaymentNotificationClient === 'function') {
                console.log('Inicializando SignalR para orden:', orderId);
                // Iniciar conexión SignalR y unirse al grupo de la orden
                startQrPaymentNotificationClient(orderId);
            } else {
                console.error('No se pudo inicializar SignalR: orderId o función no disponible');
            }
        }

        console.log('Vista QR cargada exitosamente');
    })
    .catch(error => {
        // Mostrar error si no se pudo generar el QR
        console.error('Error:', error);
        cartBody.innerHTML = `
            <div class="text-center py-5">
                <i class="bi bi-exclamation-triangle display-1 text-danger"></i>
                <h4 class="mt-3 text-danger">Error</h4>
                <p class="text-muted">No se pudo generar el código QR. Intenta nuevamente.</p>
                <button type="button" class="btn btn-primary mt-3" onclick="loadCartContent()">
                    <i class="bi bi-arrow-left"></i> Volver al Carrito
                </button>
            </div>
        `;
    });
}

/**
 * Cancela el checkout QR y vuelve al carrito.
 *
 * Esta función:
 * 1. Limpia la conexión SignalR (para no dejar conexiones huérfanas)
 * 2. Recarga el contenido del carrito
 *
 * Es llamada desde el botón "Cancelar" en la vista _Qr.cshtml
 */
function cancelQrCheckout() {
    // Limpiar la conexión SignalR si está activa
    // Esto evita que queden conexiones abiertas innecesariamente
    if (typeof cleanupQrPaymentNotification === 'function') {
        cleanupQrPaymentNotification();
    }

    // Recargar el contenido normal del carrito
    loadCartContent();
}