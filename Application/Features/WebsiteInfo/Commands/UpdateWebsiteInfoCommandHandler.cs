using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Commands;

public class UpdateWebsiteInfoCommandHandler(IWebsiteInfoRepository repository) : IRequestHandler<UpdateWebsiteInfoCommand>
{
    private readonly IWebsiteInfoRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task Handle(UpdateWebsiteInfoCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id);
        if (existing == null)
        {
            throw new ArgumentException("WebsiteInfo not found.");
        }

        existing.Name = request.Name;
        existing.Slogan = request.Slogan;
        existing.Introduction = request.Introduction;
        existing.Email = request.Email;
        existing.PhoneNumber = request.PhoneNumber;
        existing.TaxNumber = request.TaxNumber;
        existing.Location = request.Location;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
    }
}
