using QRCoder;
using static QRCoder.QRCodeGenerator;

namespace poc_mercadopago.Infrastructure.QRCode
{
    public class QrCodeGenerator : IQrCodeGenerator
    {
        private readonly ILogger<QrCodeGenerator> _logger;

        public QrCodeGenerator(ILogger<QrCodeGenerator> logger)
        {
            _logger = logger;
        }

        public string GenerateQrImageBase64(string qrData, int pixelsPerModule = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qrData))
                {
                    _logger.LogWarning("El dato para generar el QR es nulo o vacío.");
                    throw new ArgumentException("El dato para generar el QR no puede ser nulo o vacío.", nameof(qrData));
                }
                // Crear generador de QR
                using var qrGenerator = new QRCodeGenerator();

                // Generar datos del QR con nivel de corrección de error Medium
                // ECC Level M = 15% - nivel de corrección de errores (balance entre tamaño y robustez)
                using var qrCodeData = qrGenerator.CreateQrCode(qrData, ECCLevel.M);

                //Crear render de PNG
                using var qrCode = new PngByteQRCode(qrCodeData);
                //Generar bytes de la imagen png
                byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

                // Convertir a Base64 con prefijo data URI
                string base64 = Convert.ToBase64String(qrCodeBytes);
                string dataUri = $"data:image/png;base64,{base64}";

                _logger.LogInformation(
                    "QR generado exitosamente. Tamaño: {Size} bytes, Módulos: {Modules}px",
                    qrCodeBytes.Length,
                    pixelsPerModule
                );

                return dataUri;
            }
            catch (System.Exception)
            {
                _logger.LogError("Error al generar el código QR.");
                throw;
            }



        }
    }
}