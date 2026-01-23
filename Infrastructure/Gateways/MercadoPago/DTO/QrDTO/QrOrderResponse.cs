using System.Text.Json.Serialization;

namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.DTO.QrDTO
{
    /// <summary>
    /// Response al crear una orden QR dinámica en Mercado Pago.
    /// Endpoint: POST /instore/orders/qr/seller/collectors/{user_id}/pos/{external_pos_id}/qrs
    ///
    /// Esta respuesta contiene los datos necesarios para mostrar el QR al usuario.
    /// </summary>
    public sealed record QrOrderResponse
    {
        /// <summary>
        /// String EMV que codifica toda la información del pago.
        ///
        /// Este string es el dato que se codifica en la imagen QR.
        /// Cuando el usuario escanea el QR con la app de MP, la app
        /// decodifica este string y muestra los detalles del pago.
        ///
        /// Formato: EMV Co-standard (estándar para códigos QR de pago).
        /// Ejemplo: "00020101021226870014br.gov.bcb.pix2565qrcodepix..."
        ///
        /// Para mostrar al usuario, este string se convierte a imagen PNG
        /// usando QRCoder o similar (ver QrCodeGenerator.cs).
        /// </summary>
        [JsonPropertyName("qr_data")]
        public string QrData { get; set; }

        /// <summary>
        /// ID de la orden en el sistema in-store de Mercado Pago.
        ///
        /// NOTA: Este ID es diferente del merchant_order_id que llega en el webhook.
        /// - in_store_order_id: UUID generado al crear el QR
        /// - merchant_order_id: ID numérico que llega en el webhook
        ///
        /// Guardamos este ID en nuestra orden local por si necesitamos
        /// consultar o cancelar la orden posteriormente.
        /// </summary>
        [JsonPropertyName("in_store_order_id")]
        public string InStoreOrderId { get; set; }
    }


}