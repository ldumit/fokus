namespace Fokus.API.Auth;

public static class AuthMiddleware
{
    public static WebApplication UseFokusAuth(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
