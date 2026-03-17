using StackExchange.Redis;

namespace poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Services
{
    public sealed class RedisWebhookIdempotencyService : IWebhookIdempotencyService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisWebhookIdempotencyService> _logger;

        // Tiempo de vida de cada entrada 
        // (24 horas es suficiente, MP no reintenta después de eso)
        private readonly TimeSpan _ttl = TimeSpan.FromHours(24);

        // Prefijo para las claves en Redis (evita colisiones con otras keys)
        private const string RedisKeyPrefix = "webhook:idempotency:";

        public RedisWebhookIdempotencyService(IConnectionMultiplexer redis, ILogger<RedisWebhookIdempotencyService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<bool> HasBeenProcessedAsync(string notificationId)
        {
            var db = _redis.GetDatabase();
            string redisKey = RedisKeyPrefix + notificationId;
            return await db.KeyExistsAsync(redisKey);
        }


        public async Task<bool> TryProcessAsync(string notificationId)
        {
            var db = _redis.GetDatabase();
            string redisKey = RedisKeyPrefix + notificationId;

            // SETNX (SET if Not eXists) + TTL en una sola operación atómica
            // Retorna true si se creó la key (primera vez)
            // Retorna false si ya existía (duplicado)

            var wasSet = await db.StringSetAsync(
                key: redisKey,
                value: DateTime.UtcNow.ToString("O"), // Guardamos el timestamp como valor (opcional, para debugging)
                expiry: _ttl,
                when: When.NotExists
            );
            
            if (!wasSet)
            {
                _logger.LogInformation(
                    "Notificación {NotificationId} ignorada (duplicado en Redis)",
                    notificationId
                );
                return wasSet;
            }

            _logger.LogDebug(
                    "Notificación {NotificationId} registrada en Redis (TTL: {Ttl})",
                    notificationId,
                    _ttl
            );

            return wasSet;


        }
    }
}