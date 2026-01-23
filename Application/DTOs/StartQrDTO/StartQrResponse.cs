namespace poc_mercadopago.Application.DTOs.StartQrDTO
{
    /// <summary>
    /// Response del servicio StartQrAsync.
    /// Contiene todos los datos necesarios para mostrar el QR al usuario.
    ///
    /// Este DTO se usa para:
    /// 1. Construir el QrViewModel en el controlador
    /// 2. Renderizar la vista _Qr.cshtml
    /// </summary>
    public sealed class StartQrResponse
    {
        /// <summary>
        /// ID de la orden en nuestro sistema local.
        /// Este es el mismo ID que se envió como external_reference a MP.
        /// Se usa para la conexión SignalR (grupo por orderId).
        /// </summary>
        public string OrderId { get; init; }

        /// <summary>
        /// ID de la orden en el sistema in-store de Mercado Pago.
        /// Es un UUID generado por MP al crear el QR.
        /// Diferente del merchant_order_id que llega en el webhook.
        /// </summary>
        public string InStoreOrderId { get; init; }

        /// <summary>
        /// String EMV que codifica la información del pago.
        /// Este es el dato "crudo" que retorna MP.
        /// Se incluye por si el cliente necesita el dato original.
        /// </summary>
        public string QrData { get; init; } = string.Empty;

        /// <summary>
        /// Imagen QR en formato Data URI (Base64).
        /// Formato: "data:image/png;base64,iVBORw0KGgo..."
        ///
        /// Lista para usar directamente en HTML:
        /// <img src="@QrImageBase64" />
        ///
        /// Generada por QrCodeGenerator a partir de QrData.
        /// </summary>
        public string QrImageBase64 { get; init; } = string.Empty;

        /// <summary>
        /// Total de la orden a pagar.
        /// Se muestra al usuario junto con el QR.
        /// </summary>
        public decimal Total { get; init; }

        /// <summary>
        /// Código de moneda (ISO 4217).
        /// Default: "ARS" para pesos argentinos.
        /// </summary>
        public string CurrencyId { get; init; } = "ARS";
    }
}