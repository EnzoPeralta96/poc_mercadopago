//============================================================
// CLIENTE SIGNALR PARA NOTIFICACIONES DE PAGO EN TIEMPO REAL
// ============================================================

/**
 * Clase que maneja la conexión SignalR para recibir
 * notificaciones de pago en tiempo real.
*/

class QrPaymentNotificationClient {
    constructor() {
        // Referencia a la conexión SignalR
        this.connection = null;

        // OrderId que estamos monitoreando
        this.currendOrderId = null;

        //Callback para eventos
        this.onPaymentCompleted = null;
        this.onError = null;
    }

    /** 
        * Inicia la conexión SignalR y se suscribe a las notificaciones de una orden.
        * @param {string} orderId - ID de la orden a monitorear
        * @param {Function} onPaymentCompleted - Callback cuando el pago se completa
        * @param {Function} onError - Callback para errores (opcional)
    */

    async start(orderId, onPaymentCompleted, onError = null) {
        try {
            this.currendOrderId = orderId;
            this.onPaymentCompleted = onPaymentCompleted;
            this.onError = onError;

            // Crear conexión al Hub
            // La URL debe coincidir con la configurada en Program.cs
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/payment-notification")
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Information)
                .build();

            //registrar manejadores de enventos ANTES de conectar
            this._registerEventHandlers();

            await this.connection.start();
            console.log("Conexión SignalR iniciada.");

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
     * El servidor llamará a estos métodos desde el hub tipado.
     */

    _registerEventHandlers() {
        // Manejador para notificaciones de pago completado
        this.connection.on("PaymentCompleted", (notification) => {
            console.log("Notificación de pago recibida:", notification);

            // El objeto notification tiene la siguiente estructura:
            // {
            //   orderId: string,
            //   status: string,
            //   paymentId: number | null,
            //   timestamp: string,
            //   message: string
            // }
            if (this.onPaymentCompleted) {
                this.onPaymentCompleted(notification);
            }
        });

        // Evento: Conexión cerrada
        this.connection.onclose((error) => {
            console.warn("Conexión SignalR cerrada:", error);
            if (error && this.onError) {
                this.onError(error);
            }
        });

        // Evento: Reconectando
        this.connection.onreconnecting((error) => {
            console.warn("Reconectando a SignalR...", error);
        });

        // Evento: Reconectado
        this.connection.onreconnected(async (connectionId) => {
            console.log("Reconectado a SignalR. ConnectionId:", connectionId);
            if (this.currendOrderId) {
                try {
                    await this.connection.invoke("JoinOrderGroup", this.currendOrderId);
                    console.log(`Reunido al grupo de la orden ${this.currendOrderId} tras reconexión.`);
                } catch (error) {
                    console.error("Error al unirse al grupo tras reconexión:", error);
                }
            }
        });
    }


    /**
   * Cierra la conexión SignalR.
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

//Instancia global (se inicializa cuando se muestra el QR)

let qrPaymentClient = null;

/**
 * Funcion llamada cuando se muestra el QR al usuario
 * Inicializa SingalR y empieza a escuchar notificaciones
 * @param {string} orderId - ID de la orden a monitorear
 * 
 */
function startQrPaymentNotificationClient(orderId) {
    if (!qrPaymentClient) {
        qrPaymentClient = new QrPaymentNotificationClient();
    }

    const handlePaymentCompleted = (notification) => {
        console.log("Pago completado:", notification);

        const status = notification.status.toLowerCase();

        if(notification.message){
            showToast(notification.message, status === 'approved' || status === 'paid' || status === 'closed' ? 'success' : 'info');
        }

        if (status === 'approved' || status === 'paid' || status === 'closed') {

            //Pago exitoso - Redirigir a la página de éxito
            setTimeout(() => {
                window.location.href = `/checkout/return/success?payment_id=${notification.paymentId || ''}&status=${notification.status}`;
            }, 2000); // Esperar 2 segundos para mostrar el mensaje
        } else if (status === 'rejected' || status === 'cancelled') {
            //Pago fallido - Redirigir a la página de fallo
            setTimeout(() => {
                window.location.href = `/checkout/return/failure?status=${notification.status}`;
            }, 2000); // Esperar 2 segundos para mostrar el mensaje
        }else{
            // Otros estados - Solo mostrar mensaje
            console.log(`Estado: ${notification.status}`);
        }
    };

    const handleError = (error) => {
        console.error("Error en SignalR", error);
        showToast("Error en la conexión de notificaciones de pago. Por favor, recarga la página.", "error");
    };

    qrPaymentClient.start(orderId, handlePaymentCompleted, handleError);
}

function cleanupQrPaymentNotification() {
    if (qrPaymentClient) {
        qrPaymentClient.stop();
        qrPaymentClient = null;
    }
}

//Limipiar al salir de la página
window.addEventListener("beforeunload", () => {
    cleanupQrPaymentNotification();
}); 



