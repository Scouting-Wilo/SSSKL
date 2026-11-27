using MediatR;
using SSSKL.Contracts.Dto.Api;

namespace SSSKL.Application.Features.RoleManagement.Queries.GetRoles;

/// <summary>Lists the roles.</summary>
/// <param name="IncludeProtected">True also lists the system roles (Admin, Kiosk, User, Guest).</param>
public sealed record GetRolesQuery(bool IncludeProtected) : IRequest<IReadOnlyList<RoleDto>>;
