namespace poc_mercadopago.Infrastructure.QRCode
{
    public interface IQrCodeGenerator
    {
        /// <summary>
        /// Genera una imagen QR en formato Base64 desde datos de texto.
        /// </summary>
        /// <param name="qrData">Datos a codificar en el QR (ej: string EMV de Mercado Pago)</param>
        /// <param name="pixelsPerModule">Tamaño del QR (píxeles por módulo, default: 20)</param>
        /// <returns>String Base64 con formato "data:image/png;base64,..." listo para usar en HTML</returns>
        string GenerateQrImageBase64(string qrData, int pixelsPerModule = 20);
    }
}