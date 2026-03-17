

using System.Security.Cryptography;
using System.Text;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs;

namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Services
{
    public sealed class WebhookSignatureValidator : IWebhookSignatureValidator
    {
        private readonly string? _checkoutProSecret;
        private readonly ILogger<WebhookSignatureValidator> _logger;

        // Tolerancia de tiempo: rechazar firmas muy viejas (protege contra replay attacks)
        private readonly TimeSpan _timestampTolerance = TimeSpan.FromMinutes(5);
        public WebhookSignatureValidator(IConfiguration configuration, ILogger<WebhookSignatureValidator> logger)
        {
            _checkoutProSecret = configuration.GetValue<string>("MercadoPago:WebhookSecret");
            _logger = logger;
        }

        private string? GetSecretForAppType(string? appType)
        {
            return appType switch
            {
                "checkout" => _checkoutProSecret,
                _ => null
            };
        }

        private static string ComputeHmacSha256(string data, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private bool IsSignatureValid(WebhookSignatureData data, string secretKey)
        {
            //mp le llama manifest pero es el string que se firma, construido con los campos del webhook
            string manifest = data.BuildSignatureTemplate();
            string expectedSignature = ComputeHmacSha256(manifest, secretKey);

            bool isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(data.ReceivedSignature)
            );

            if (!isValid)
            {
                _logger.LogWarning(
                    "Webhook rechazado: firma no coincide (DataId: {DataId})",
                    data.DataId
                );
            }

            return isValid;
        }

        private bool IsTimestampValid(DateTimeOffset signatureTime)
        {

            var age = DateTimeOffset.UtcNow - signatureTime;
            var absoluteAge = age.Duration();

            if (absoluteAge > _timestampTolerance)
            {
                _logger.LogWarning("Webhook rechazado: timestamp fuera de tolerancia ({Age})", age);
                return false;
            }

            return true;
        }

        public bool Validate(string? appType, string? xSignature, string? xRequestId, string? dataId)
        {
            // Las notificaciones de QR Dinámico no soportan validación por firma según la doc de MP:
            // "notificaciones de Código QR no pueden ser validadas utilizando la clave secreta"
            // La validación del origen se delega a la consulta posterior a la API de MP.
            if (appType == "qr")
            {
                _logger.LogInformation("Webhook QR: validación de firma omitida (no soportada por MP para QR)");
                return true;
            }

            var secret = GetSecretForAppType(appType);

            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("Webhook rechazado: appType desconocido o ausente ({AppType})", appType);
                return false;
            }

            var signatureData = WebhookSignatureData.TryCreate(xSignature, xRequestId, dataId);

            if (signatureData is null)
            {
                _logger.LogWarning(
                    "Webhook rechazado: encabezado de firma inválido o ausente (DataId: {DataId})",
                    dataId
                );
                return false;
            }

            if (!IsTimestampValid(signatureData.TimestampAsDateTime)) return false;

            return IsSignatureValid(signatureData, secret);
        }


    }
}