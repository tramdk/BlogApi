using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Commands;

public class CreateWebsiteInfoCommandHandler : IRequestHandler<CreateWebsiteInfoCommand, Guid>
{
    private readonly IWebsiteInfoRepository _repository;

    public CreateWebsiteInfoCommandHandler(IWebsiteInfoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateWebsiteInfoCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            throw new ArgumentException("Name cannot be null or empty.");
        }

        var websiteInfo = new Domain.Entities.WebsiteInfo
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slogan = request.Slogan,
            Introduction = request.Introduction,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            TaxNumber = request.TaxNumber,
            Location = request.Location,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(websiteInfo);
    }
}
