using System.Net;
using System.Net.Mail;

namespace PlataformaTGA.Auth;

// Correos de la Plataforma: enlace de recuperación de acceso (Figma 05-06) y contraseña inicial o
// temporal de una cuenta (Usuarios U2-U3).
// Dos modos:
//   SMTP: con Smtp:Host y Smtp:From configurados, envía el correo. Es el único modo válido en producción.
//   Desarrollo: sin SMTP y en Development, el mensaje se escribe en el log del backend en vez de enviarse.
// El modo desarrollo depende de IsDevelopment() a propósito: en producción, escribir enlaces o contraseñas en un
// log permitiría secuestrar cuentas a quien lea los registros. Fuera de Development y sin SMTP, no se envía nada.
public class Correo(IConfiguration config, IHostEnvironment entorno, ILogger<Correo> logger)
{
    // En desarrollo el frontend corre aquí; en producción se configura Seguridad:UrlPublica.
    const string UrlDesarrollo = "http://localhost:3000";

    bool SmtpConfigurado => !string.IsNullOrWhiteSpace(config["Smtp:Host"]) && !string.IsNullOrWhiteSpace(config["Smtp:From"]);

    string? UrlPublica =>
        Uri.TryCreate(config["Seguridad:UrlPublica"], UriKind.Absolute, out var u) ? u.ToString()
        : entorno.IsDevelopment() ? UrlDesarrollo
        : null;

    bool ModoDesarrollo => !SmtpConfigurado && entorno.IsDevelopment();

    public bool Configurado => UrlPublica != null && (SmtpConfigurado || ModoDesarrollo);

    string Url(string ruta) => UrlPublica!.TrimEnd('/') + ruta;

    // El token va después de «#»: el navegador no lo manda al servidor, así no queda en registros ni en Referer.
    public Task EnviarRecuperacion(string para, string token) =>
        Enviar(para, "Recuperación de acceso",
            "Restablece tu contraseña con este enlace (vence en 30 minutos y solo se usa una vez):\n" +
            Url("/recuperar#token=" + token) +
            "\nSi no lo solicitaste, ignora este mensaje.");

    // egresado: entra por su propio portal.
    public Task EnviarContrasena(string para, string nombre, string contrasena, bool egresado) =>
        Enviar(para, "Tu acceso a la Plataforma TGA",
            $"Hola, {nombre}.\n\nTu contraseña temporal es: {contrasena}\n" +
            $"Entra en {Url(egresado ? "/acceso-egresados" : "/")} con tu correo ({para}) y esta contraseña; " +
            "al entrar se te pedirá cambiarla.");

    async Task Enviar(string para, string asunto, string cuerpo)
    {
        if (ModoDesarrollo)
        {
            logger.LogWarning(
                "\n┌─────────────────────────────────────────────────────────────\n" +
                "│ MODO DESARROLLO · el correo NO se envió\n" +
                "│ Para:   {Para}\n" +
                "│ Asunto: {Asunto}\n" +
                "│\n{Cuerpo}\n" +
                "│\n│ Configura Smtp:Host y Smtp:From para enviarlo de verdad.\n" +
                "└─────────────────────────────────────────────────────────────",
                para, asunto, string.Join('\n', cuerpo.Split('\n').Select(l => "│ " + l)));
            return;
        }

        using var cliente = new SmtpClient(config["Smtp:Host"], config.GetValue("Smtp:Port", 587))
        {
            EnableSsl = config.GetValue("Smtp:EnableSsl", true),
            Credentials = new NetworkCredential(config["Smtp:User"], config["Smtp:Password"]),
        };
        using var mensaje = new MailMessage(config["Smtp:From"]!, para, asunto, cuerpo);
        await cliente.SendMailAsync(mensaje);
    }
}
