using System.Collections.Generic;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Queries;

public record GetAllWebsiteInfoQuery() : IRequest<IEnumerable<FloraCore.Domain.Entities.WebsiteInfo>>;
