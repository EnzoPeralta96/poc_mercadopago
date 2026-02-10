
using System.ComponentModel.DataAnnotations;

namespace poc_mercadopago.Infrastructure.Configuration
{
    public sealed class MercadoPagoOptions
    {
        public const string SectionName = "MercadoPago";

        [Required(ErrorMessage = "El AccessToken es obligatorio.")]
        [MinLength(20, ErrorMessage = "MercadoPago AccessToken debe tener al menos 20 caracteres.")]
        public string AccessToken { get; set; }

        [Required(ErrorMessage = "El PublicKey es obligatorio.")]
        [MinLength(20, ErrorMessage = "MercadoPago PublicKey debe tener al menos 20 caracteres.")]
        public string PublicKey { get; set; }   

        [Required(ErrorMessage = "La BaseUrl es obligatoria.")]
        [Url(ErrorMessage = "La BaseUrl debe ser una URL válida.")]
        public string BaseUrl { get; set; } 

        [Required]
        public string IntegratorId {get; set;}

    }
}