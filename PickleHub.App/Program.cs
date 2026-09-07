using PickleHub.Authen;
using PickleHub.Catalog;
using PickleHub.Customers;
using PickleHub.System;
using PickleHub.Inventory;
using PickleHub.AuditLog;
using PickleHub.CartOrder;
using PickleHub.Payment;
using PickleHub.Notification;
using PickleHub.Review;
using PickleHub.Blog;
using PickleHub.Gateway;

namespace PickleHub.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================================");
        Console.WriteLine(" [PickleHub.App] Starting Modular Monolith Host in Single Process ");
        Console.WriteLine("==================================================================");

        // Ensure ASPNETCORE_URLS does not hijack internal loopback ports
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);

        var portStr = Environment.GetEnvironmentVariable("PORT");
        int gatewayPort = int.TryParse(portStr, out var p) ? p : 8080;

        var apps = new List<WebApplication>
        {
            await AuthenApp.BuildAsync(args, 5001),
            await CatalogApp.BuildAsync(args, 5002),
            await CustomersApp.BuildAsync(args, 5003),
            await SystemApp.BuildAsync(args, 5004),
            await InventoryApp.BuildAsync(args, 5005),
            await AuditLogApp.BuildAsync(args, 5006),
            await CartOrderApp.BuildAsync(args, 5007),
            await PaymentApp.BuildAsync(args, 5008),
            await NotificationApp.BuildAsync(args, 5009),
            await ReviewApp.BuildAsync(args, 5010),
            await BlogApp.BuildAsync(args, 5011),
            await GatewayApp.BuildAsync(args, gatewayPort)
        };

        foreach (var app in apps)
        {
            await app.StartAsync();
            Console.WriteLine($"[HOST] Module online: {app.Environment.ApplicationName} listening on {string.Join(", ", app.Urls)}");
        }

        Console.WriteLine("==================================================================");
        Console.WriteLine($" [PickleHub.App] All 12 modules online! Gateway listening on {gatewayPort}");
        Console.WriteLine("==================================================================");

        var tcs = new TaskCompletionSource();
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            Console.WriteLine("[HOST] Shutting down modules gracefully...");
            foreach (var app in apps)
            {
                try { app.StopAsync().GetAwaiter().GetResult(); } catch {}
            }
            tcs.TrySetResult();
        };

        await tcs.Task;
    }
}
