namespace SGR.Api.Models.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UsuarioDto Usuario { get; set; } = null!;
    public PerfilDto Perfil { get; set; } = null!;
}

