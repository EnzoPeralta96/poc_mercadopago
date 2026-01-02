using poc_mercadopago.Infrastructure.Cart.DTOs;
using poc_mercadopago.Infrastructure.Session;

namespace poc_mercadopago.Infrastructure.Cart.CartStore
{
    public class SessionCartStore : ICartStore
    {
        private const string CartKey = "CART_KEY";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionCartStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session
          ?? throw new InvalidOperationException("No HttpContext available to access Session.");

        public Task ClearCartAsync()
        {
            Session.Remove(CartKey);
            return Task.CompletedTask;
        }

        public Task<SessionCartDTO> GetCartAsync()
        {
            var cart = Session.GetObjectFromJson<SessionCartDTO>(CartKey) ?? new SessionCartDTO();
            return Task.FromResult(cart);
        }

        public Task SaveCartAsync(SessionCartDTO cart)
        {
            Session.SetObjectAsJson(CartKey, cart);
            return Task.CompletedTask;
        }
    }
}