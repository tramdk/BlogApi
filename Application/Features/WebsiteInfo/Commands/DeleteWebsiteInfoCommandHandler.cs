using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Commands;

public class DeleteWebsiteInfoCommandHandler : IRequestHandler<DeleteWebsiteInfoCommand>
{
    private readonly IWebsiteInfoRepository _repository;

    public DeleteWebsiteInfoCommandHandler(IWebsiteInfoRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteWebsiteInfoCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id);
        if (existing == null)
        {
            throw new ArgumentException("WebsiteInfo not found.");
        }

        await _repository.DeleteAsync(request.Id);
    }
}
