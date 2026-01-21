using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace poc_mercadopago.Application.DTOs.StartQrDTO
{
    public sealed class StartQrRequest
    {
        public List<CartItemRequestDTO> Items { get; set; } = [];
    }
}