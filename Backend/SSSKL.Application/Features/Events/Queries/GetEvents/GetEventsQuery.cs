using MediatR;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Events.Queries.GetEvents;

/// <summary>A page of events.</summary>
/// <param name="UserId">The viewing user, or null for the public listing. Role-restricted events are filtered for this user.</param>
public sealed record GetEventsQuery(int Skip, int Take, bool FutureOnly, string? UserId, string? RequiredRole) : IRequest<PaginationObject<EventDto>>;
