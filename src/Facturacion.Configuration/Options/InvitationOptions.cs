namespace Facturacion.Configuration.Options;

/// <summary>Invitaciones entre usuarios del mismo tenant.</summary>
public sealed class InvitationOptions
{
    public const string SectionName = "Invitations";

    /// <summary>Vigencia del enlace de invitacion.</summary>
    public int ExpirationHours { get; set; } = 48;

    /// <summary>Destino del enlace. Debe ser HTTPS.</summary>
    public string AcceptUrl { get; set; } = "";
}