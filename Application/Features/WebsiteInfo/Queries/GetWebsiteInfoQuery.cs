using System;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Queries;

public record GetWebsiteInfoQuery(Guid Id) : IRequest<FloraCore.Domain.Entities.WebsiteInfo?>;
