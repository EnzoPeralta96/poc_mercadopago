using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using poc_mercadopago.Infrastructure.Cart.DTOs;

namespace poc_mercadopago.Infrastructure.Cart.CartStore
{
    public interface ICartStore
    {
        Task<SessionCartDTO> GetCartAsync();
        Task SaveCartAsync(SessionCartDTO cart);
        Task ClearCartAsync();
    }
}