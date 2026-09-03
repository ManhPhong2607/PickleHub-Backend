namespace PickleHub.Gateway.Middleware
{
    public class JwtForwardingMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtForwardingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            return _next(context);
        }
    }
}
