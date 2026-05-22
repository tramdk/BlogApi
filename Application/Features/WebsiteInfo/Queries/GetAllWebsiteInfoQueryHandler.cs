using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Queries;

public class GetAllWebsiteInfoQueryHandler : IRequestHandler<GetAllWebsiteInfoQuery, IEnumerable<FloraCore.Domain.Entities.WebsiteInfo>>
{
    private readonly IWebsiteInfoRepository _repository;

    public GetAllWebsiteInfoQueryHandler(IWebsiteInfoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FloraCore.Domain.Entities.WebsiteInfo>> Handle(GetAllWebsiteInfoQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync();
    }
}
