namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.DTOs
{
    public sealed class WebhookSignatureData
    {
        /// Timestamp Unix de la firma.
        public long Timestamp { get; init; }

        /// Firma recibida (v1).
        public string ReceivedSignature { get; init; }

        /*
            Son nullables por diseño. Mercado Pago no siempre envía estos valores. El template de firma se construye dinámicamente según qué datos estén presentes:
        */
        
        /// Request ID del header x-request-id.
        public string? RequestId { get; init; }

        /// Data ID del query string.
        public string? DataId { get; init; }

        /// Momento en que se generó la firma.
        public DateTime TimestampAsDateTime => DateTimeOffset.FromUnixTimeSeconds(Timestamp).UtcDateTime;

        private WebhookSignatureData(long timestamp, string receivedSignature, string? requestId, string? dataId)
        {
            Timestamp = timestamp;
            ReceivedSignature = receivedSignature;
            RequestId = requestId;
            DataId = dataId;
        }

        /*
       "ts=1704067200,v1=abc123"
           │
           ▼ Split(',')
       ["ts=1704067200", "v1=abc123"]
           │
           ▼ Select(Split('=', 2))
       [["ts","1704067200"], ["v1","abc123"]]
           │
           ▼ Where(Length == 2)
       [["ts","1704067200"], ["v1","abc123"]]  (sin cambios)
           │
           ▼ ToDictionary([0], [1])
       { "ts": "1704067200", "v1": "abc123" }
       */
        private static Dictionary<string, string> ParseSignatureHeader(string signature)
        {
            return signature
                    .Split(',')
                    .Select(part => part.Split('=', 2))
                    .Where(pair => pair.Length == 2)
                    .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());
        }

        /// <summary>
        /// Intenta crear el DTO parseando el header x-signature.
        /// Retorna null si los datos son inválidos.
        /// </summary>
    
      
        public static WebhookSignatureData? TryCreate(string? xSignature, string? xRequestId, string? dataId)
        {
            if (string.IsNullOrEmpty(xSignature)) return null;

            var parts = ParseSignatureHeader(xSignature);

            if (!parts.TryGetValue("ts", out var tsValue) ||
                !parts.TryGetValue("v1", out var signatureValue))
                return null;

            if (!long.TryParse(tsValue, out var timestamp))
                return null;

            if (string.IsNullOrEmpty(signatureValue))
                return null;

            return new WebhookSignatureData(timestamp, signatureValue, xRequestId, dataId);
        }

        public string BuildSignatureTemplate()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(DataId))
                parts.Add($"id:{DataId.ToLowerInvariant()}");

            if (!string.IsNullOrEmpty(RequestId))
                parts.Add($"request-id:{RequestId}");

            parts.Add($"ts:{Timestamp}");

            return string.Join(";", parts) + ";";
        }
    }
}