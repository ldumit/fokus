using Fokus.API.Features.Auth.GetUsers;

namespace Fokus.API.Features.Auth.UpdateRole;

public class UpdateRoleRequest
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class UpdateRoleRequestValidator : Validator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("User ID must be greater than 0.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role is required.")
            .Must(r => r == AccessRoles.Admin || r == AccessRoles.Manager)
            .WithMessage("Role must be 'Admin' or 'Manager'.");
    }
}
