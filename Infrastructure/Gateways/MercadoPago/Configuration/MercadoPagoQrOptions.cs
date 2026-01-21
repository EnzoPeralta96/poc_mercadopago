
using System.ComponentModel.DataAnnotations;


namespace poc_mercadopago.Infrastructure.Gateways.MercadoPago.Configuration
{
    /// <summary>
    /// Opciones de configuración para la integración de QR Dinámico con Mercado Pago.
    /// Esta clase es SEPARADA de MercadoPagoOptions porque usa credenciales de una aplicación diferente.
    /// </summary>
    public sealed class MercadoPagoQrOptions
    {
        public const string SectionName = "MercadoPagoQr";

        [Required(ErrorMessage = "El AccessToken para QR es obligatorio")]
        [MinLength(20, ErrorMessage = "El AccessToken para QR debe tener al menos 20 caracteres")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "El PublicKey para QR es obligatorio")]
        [MinLength(20, ErrorMessage = "El PublicKey para QR debe tener al menos 20 caracteres")]
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// ID del usuario vendedor en Mercado Pago.
        /// Este ID se usa en la URL del endpoint de QR.
        /// Ejemplo: 3068452461
        /// </summary>
        [Required(ErrorMessage = "El UserId para QR es obligatorio")]
        public string UserId { get; set; } = string.Empty;

        // <summary>
        /// ID externo del punto de venta (POS).
        /// Debe corresponder al external_id usado al crear el POS.
        /// Ejemplo: "SUC001POS001"
        [Required(ErrorMessage = "El ExternalPosId es obligatorio")]
        [MinLength(1, ErrorMessage = "El ExternalPosId no puede estar vacío")]
        public string ExternalPosId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La BaseUrl para QR es obligatoria")]
        [Url(ErrorMessage = "La BaseUrl debe ser una URL válida")]
        public string BaseUrl { get; set; } = string.Empty;

    }
}