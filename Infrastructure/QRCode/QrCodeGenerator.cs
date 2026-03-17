using QRCoder;
using static QRCoder.QRCodeGenerator;

namespace poc_mercadopago.Infrastructure.QRCode
{
    /// <summary>
    /// Generador de imágenes QR a partir de datos de texto.
    ///
    /// Este servicio convierte el string qr_data de Mercado Pago (formato EMV)
    /// en una imagen PNG codificada en Base64 lista para mostrar en HTML.
    ///
    /// Uso:
    /// El qr_data de MP es un string como "00020101021226870014br.gov.bcb.pix..."
    /// Este servicio lo convierte a "data:image/png;base64,iVBORw0KGgo..."
    /// que puede usarse directamente en un <img src="...">.
    ///
    /// Dependencia: QRCoder (NuGet package)
    /// </summary>
    public class QrCodeGenerator : IQrCodeGenerator
    {
        private readonly ILogger<QrCodeGenerator> _logger;

        public QrCodeGenerator(ILogger<QrCodeGenerator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Genera una imagen QR en formato PNG codificada en Base64.
        ///
        /// Proceso:
        /// 1. Recibe el qr_data (string EMV) de Mercado Pago
        /// 2. Genera la matriz QR usando QRCoder
        /// 3. Renderiza a imagen PNG
        /// 4. Codifica en Base64
        /// 5. Agrega prefijo data URI para uso en HTML
        ///
        /// El resultado puede usarse directamente en HTML:
        /// <img src="@result" />
        /// </summary>
        /// <param name="qrData">String EMV retornado por la API de MP (qr_data)</param>
        /// <param name="pixelsPerModule">
        /// Tamaño del QR en píxeles por módulo.
        /// Un módulo es cada "cuadradito" del QR.
        /// Valor típico: 20 (resulta en un QR de ~500-600px de lado)
        /// </param>
        /// <returns>Data URI listo para usar en <img src="..."></returns>
        public string GenerateQrImageBase64(string qrData, int pixelsPerModule = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qrData))
                {
                    _logger.LogWarning("El dato para generar el QR es nulo o vacío.");
                    throw new ArgumentException("El dato para generar el QR no puede ser nulo o vacío.", nameof(qrData));
                }

                // Crear instancia del generador de QR
                /*
                    Es el motor que calcula la matriz de datos (dónde van los puntos negros y blancos). 
                */
                using var qrGenerator = new QRCodeGenerator();

                // Generar la matriz de datos del QR
                /*
                Su estructura: Es literalmente una colección de bools (Verdadero/Falso).
                    Data.ModuleMatrix[0][0] = true (Aquí va un punto)./
                    Data.ModuleMatrix[0][1] = false (Aquí va espacio vacío).
                */
                //ECC Level: Error Correcion Level
                // ECC Level M = 15% de corrección de errores
                // Es un balance entre:
                // - L (7%): QR más pequeño pero menos tolerante a daños
                // - M (15%): Balance recomendado para la mayoría de casos
                // - Q (25%): Más tolerante pero QR más grande
                // - H (30%): Máxima tolerancia pero QR muy grande

                using var qrCodeData = qrGenerator.CreateQrCode(qrData, ECCLevel.M);

                // Crear renderer para PNG (sin dependencia de System.Drawing)
                // PngByteQRCode usa librerías nativas y es cross-platform
                /*
                    este componente escribe manualmente los bytes 
                    que componen un archivo PNG (cabeceras, 
                    chunks de datos, cierre).
                */
                using var qrCode = new PngByteQRCode(qrCodeData);

                // Generar los bytes de la imagen PNG
                // pixelsPerModule determina el tamaño final del QR
                byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

                // Convertir a Base64
                string base64 = Convert.ToBase64String(qrCodeBytes);

                // Agregar prefijo data URI para uso directo en HTML
                // Formato: data:image/png;base64,{datos}
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