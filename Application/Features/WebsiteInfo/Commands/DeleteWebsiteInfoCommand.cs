using System;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Commands;

public record DeleteWebsiteInfoCommand(Guid Id) : IRequest;
