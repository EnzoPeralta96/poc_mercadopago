using poc_mercadopago.Application.Services.PaymentService;
using poc_mercadopago.Infrastructure.Cart.CartStore;
using poc_mercadopago.Infrastructure.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.Configuration;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoGateway;
using poc_mercadopago.Infrastructure.Gateways.MercadoPago.MercadoPagoQRGateway;
using poc_mercadopago.Infrastructure.QRCode;
using poc_mercadopago.Infrastructure.SignalR.Hub;
using poc_mercadopago.Infrastructure.SignalR.NotificationService;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Handlers;
using poc_mercadopago.Infrastructure.Webhooks.MercadoPago.Services;
using poc_mercadopago.Repository.OrderRepository;
using poc_mercadopago.Repository.ProductRepository;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}.cshtml");
    });
    
//ContexAccesor para usar en CartStore
builder.Services.AddHttpContextAccessor();

//Session config
builder.Services.AddDistributedMemoryCache();

//HttpClient Factory para el gateway
builder.Services.AddHttpClient();

builder.Services.AddSignalR();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//Carrito en Session
builder.Services.AddScoped<ICartStore, SessionCartStore>();


//Repositorios
builder.Services.AddSingleton<IProductRepository, JsonProductRepository>();
builder.Services.AddScoped<IOrderRepository, JsonOrderRepository>();

//Servicios
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentNotificationService, PaymentNotificationService>();


//Configuracion de mercadopago
//Registrar la configuracion de mercado pago
builder.Services.Configure<MercadoPagoOptions>(
    builder.Configuration.GetSection(MercadoPagoOptions.SectionName)
);

//Validar la configuracion de mercado pago al inicio de la aplicacion
builder.Services.AddOptions<MercadoPagoOptions>()
    .BindConfiguration(MercadoPagoOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

//Configuracion de mercadopago QR
//Registrar la configuracion de mercado pago QR
builder.Services.Configure<MercadoPagoQrOptions>(
    builder.Configuration.GetSection(MercadoPagoQrOptions.SectionName)
);

//Validar la configuracion de mercado pago QR al inicio de la aplicacion
builder.Services.AddOptions<MercadoPagoQrOptions>()
    .BindConfiguration(MercadoPagoQrOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

//Mercadopago Gateway
builder.Services.AddScoped<IMercadoPagoGateway, MercadoPagoGateway>();
builder.Services.AddScoped<IMercadoPagoQRGateway, MercadoPagoQRGateway>();

//Generador de QR
builder.Services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

// Redis
var redisConnectionString = builder.Configuration["Redis:ConnectionString"]!;
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));

// Webhook services
builder.Services.AddScoped<IWebhookSignatureValidator, WebhookSignatureValidator>();
builder.Services.AddScoped<IWebhookIdempotencyService, RedisWebhookIdempotencyService>();

// Webhook handlers
builder.Services.AddScoped<IWebhookHandler, PaymentWebhookHandler>();
builder.Services.AddScoped<IWebhookHandler, MerchantOrderWebhookHandler>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapHub<PaymentNotificationHub>("/hubs/payment-notification");


app.Run();
