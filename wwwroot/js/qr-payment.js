//============================================================
// CLIENTE SIGNALR PARA NOTIFICACIONES DE PAGO QR EN TIEMPO REAL
// ============================================================
//
// Este archivo implementa la conexión SignalR del lado del cliente
// para recibir notificaciones cuando un pago QR se completa.
//
// Flujo:
// 1. Usuario ve el QR en pantalla
// 2. Este cliente se conecta a /hubs/payment-notification
// 3. El cliente se une al grupo de la orden (JoinOrderGroup)
// 4. Usuario escanea QR y paga con la app de MP
// 5. MP envía webhook al servidor
// 6. El servidor notifica al grupo vía SignalR
// 7. Este cliente recibe "PaymentCompleted"
// 8. El cliente redirige al usuario según el resultado
//
// Dependencias:
// - signalr.js debe estar cargado antes de este script
// - La función showToast() de cart.js para mostrar mensajes
//
// Uso:
// Llamar startQrPaymentNotificationClient(orderId) después de mostrar el QR
// Llamar cleanupQrPaymentNotification() al cancelar o salir
//============================================================

/**
 * Clase que maneja la conexión SignalR para recibir
 * notificaciones de pago en tiempo real.
 *
 * Esta clase encapsula toda la lógica de SignalR:
 * - Conexión y reconexión automática
 * - Suscripción a grupos por orderId
 * - Manejo de eventos del servidor
 */
class QrPaymentNotificationClient {
    constructor() {
        // Referencia a la conexión SignalR activa
        this.connection = null;

        // OrderId que estamos monitoreando (para reconexión)
        this.currendOrderId = null;

        // Callbacks para eventos
        this.onPaymentCompleted = null;
        this.onError = null;
    }

    /**
     * Inicia la conexión SignalR y se suscribe a las notificaciones de una orden.
     *
     * @param {string} orderId - ID de la orden local a monitorear
     * @param {Function} onPaymentCompleted - Callback cuando el pago se completa
     *        Recibe: { orderId, status, paymentId, timestamp, message }
     * @param {Function} onError - Callback para errores (opcional)
     */
    async start(orderId, onPaymentCompleted, onError = null) {
        try {
            this.currendOrderId = orderId;
            this.onPaymentCompleted = onPaymentCompleted;
            this.onError = onError;

            // Crear conexión al Hub SignalR
            // La URL debe coincidir con app.MapHub<>() en Program.cs
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/payment-notification")  // URL del hub
                .withAutomaticReconnect()               // Reconexión automática si se pierde conexión
                .configureLogging(signalR.LogLevel.Information)  // Nivel de log
                .build();

            // IMPORTANTE: Registrar handlers ANTES de conectar
            // Si se registran después, se pueden perder eventos
            this._registerEventHandlers();

            // Iniciar la conexión WebSocket
            await this.connection.start();
            console.log("Conexión SignalR iniciada.");

            // Unirse al grupo de la orden
            // El servidor creará el grupo "order_{orderId}" si no existe
            await this.connection.invoke("JoinOrderGroup", orderId);
            console.log(`Unido al grupo de la orden ${orderId}`);

        } catch (error) {
            console.error("Error al iniciar la conexión SignalR:", error);

            if (this.onError) {
                this.onError(error);
            }
        }
    }

    /**
     * Registra los manejadores de eventos del servidor.
     *
     * El servidor puede invocar estos métodos en cualquier momento.
     * Los nombres deben coincidir exactamente con los definidos en
     * IPaymentNotificationClient (C#).
     *
     * Eventos:
     * - PaymentCompleted: Cuando el pago cambia de estado
     * - onclose: Cuando la conexión se cierra
     * - onreconnecting: Cuando se está reconectando
     * - onreconnected: Cuando se reconectó exitosamente
     */
    _registerEventHandlers() {
        // Manejador principal: notificaciones de pago
        // El servidor llama PaymentCompleted desde PaymentNotificationService
        this.connection.on("PaymentCompleted", (notification) => {
            console.log("Notificación de pago recibida:", notification);

            // Estructura del objeto notification (viene de PaymentCompletedNotification.cs):
            // {
            //   orderId: string,      // Nuestro ID local
            //   status: string,       // Estado de MP: "closed", "opened", "rejected", etc.
            //   paymentId: number,    // ID del pago en MP (puede ser null)
            //   timestamp: string,    // Fecha/hora UTC
            //   message: string       // Mensaje amigable para mostrar
            // }
            if (this.onPaymentCompleted) {
                this.onPaymentCompleted(notification);
            }
        });

        // Evento: La conexión se cerró (ya sea por error o intencionalmente)
        this.connection.onclose((error) => {
            console.warn("Conexión SignalR cerrada:", error);
            if (error && this.onError) {
                this.onError(error);
            }
        });

        // Evento: Intentando reconectar (withAutomaticReconnect lo maneja)
        this.connection.onreconnecting((error) => {
            console.warn("Reconectando a SignalR...", error);
            // Aquí se podría actualizar la UI para mostrar "Reconectando..."
        });

        // Evento: Reconexión exitosa
        // IMPORTANTE: Al reconectarse, la suscripción al grupo se pierde
        // Debemos volver a unir al grupo
        this.connection.onreconnected(async (connectionId) => {
            console.log("Reconectado a SignalR. ConnectionId:", connectionId);
            if (this.currendOrderId) {
                try {
                    // Volver a unirse al grupo de la orden
                    await this.connection.invoke("JoinOrderGroup", this.currendOrderId);
                    console.log(`Reunido al grupo de la orden ${this.currendOrderId} tras reconexión.`);
                } catch (error) {
                    console.error("Error al unirse al grupo tras reconexión:", error);
                }
            }
        });
    }


    /**
     * Cierra la conexión SignalR de forma limpia.
     * Llamar esto cuando el usuario cancela o sale de la página.
     */
    async stop() {
        try {
            if (this.connection) {
                await this.connection.stop();
                console.log("Conexión SignalR detenida.");
            }
        } catch (error) {
            console.error("Error al detener la conexión SignalR:", error);
        }
    }
}

//============================================================
// FUNCIONES PÚBLICAS (API pública para cart.js)
//============================================================

// Instancia global del cliente SignalR
// Se inicializa cuando se muestra el QR y se limpia al cancelar
let qrPaymentClient = null;

/**
 * Función pública llamada cuando se muestra el QR al usuario.
 * Inicializa SignalR y configura los callbacks para manejar los eventos.
 *
 * Esta función es llamada desde cart.js en proceedToQr() después
 * de cargar el HTML del QR en el offcanvas.
 *
 * IMPORTANTE: Los scripts dentro de innerHTML no se ejecutan,
 * por eso cart.js llama esta función manualmente.
 *
 * @param {string} orderId - ID de la orden local a monitorear
 */
function startQrPaymentNotificationClient(orderId) {
    // Crear instancia si no existe (patrón singleton)
    if (!qrPaymentClient) {
        qrPaymentClient = new QrPaymentNotificationClient();
    }

    /**
     * Callback cuando el pago se completa (o cambia de estado).
     * Decide qué hacer según el estado recibido.
     */
    const handlePaymentCompleted = (notification) => {
        console.log("Pago completado:", notification);

        const status = notification.status.toLowerCase();

        // Mostrar toast con el mensaje amigable
        if (notification.message) {
            // IMPORTANTE: "closed" es el estado de pago exitoso para merchant_order (QR)
            // "approved" y "paid" son estados alternativos (menos comunes para QR)
            const isSuccess = status === 'approved' || status === 'paid' || status === 'closed';
            showToast(notification.message, isSuccess ? 'success' : 'info');
        }

        // Redirigir según el resultado del pago
        if (status === 'approved' || status === 'paid' || status === 'closed') {
            // PAGO EXITOSO - Redirigir a la página de éxito
            // Esperamos 2 segundos para que el usuario vea el toast
            setTimeout(() => {
                window.location.href = `/checkout/return/success?payment_id=${notification.paymentId || ''}&status=${notification.status}`;
            }, 2000);
        } else if (status === 'rejected' || status === 'cancelled') {
            // PAGO FALLIDO - Redirigir a la página de error
            setTimeout(() => {
                window.location.href = `/checkout/return/failure?status=${notification.status}`;
            }, 2000);
        } else {
            // OTROS ESTADOS (opened, pending, etc.) - Solo mostrar mensaje
            // El usuario debe seguir esperando o reintentar
            console.log(`Estado intermedio recibido: ${notification.status}`);
        }
    };

    /**
     * Callback para errores de conexión.
     */
    const handleError = (error) => {
        console.error("Error en SignalR", error);
        showToast("Error en la conexión de notificaciones de pago. Por favor, recarga la página.", "error");
    };

    // Iniciar la conexión con los callbacks configurados
    qrPaymentClient.start(orderId, handlePaymentCompleted, handleError);
}

/**
 * Limpia la conexión SignalR.
 * Llamar cuando el usuario cancela el checkout QR o navega fuera.
 *
 * Esta función es llamada desde:
 * - cart.js -> cancelQrCheckout() cuando el usuario cancela
 * - beforeunload event cuando el usuario cierra la página
 */
function cleanupQrPaymentNotification() {
    if (qrPaymentClient) {
        qrPaymentClient.stop();
        qrPaymentClient = null;
    }
}

// Limpiar automáticamente al salir de la página
// Esto evita conexiones huérfanas en el servidor
window.addEventListener("beforeunload", () => {
    cleanupQrPaymentNotification();
}); 



