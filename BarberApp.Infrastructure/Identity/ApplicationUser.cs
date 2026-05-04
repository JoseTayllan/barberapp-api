using Microsoft.AspNetCore.Identity;

namespace BarberApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;
    public Guid? BarbeiroId { get; set; } // null para Admin e Cliente
}