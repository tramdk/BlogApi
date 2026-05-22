using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Queries;

public class GetWebsiteInfoQueryHandler : IRequestHandler<GetWebsiteInfoQuery, FloraCore.Domain.Entities.WebsiteInfo?>
{
    private readonly IWebsiteInfoRepository _repository;

    public GetWebsiteInfoQueryHandler(IWebsiteInfoRepository repository)
    {
        _repository = repository;
    }

    public async Task<FloraCore.Domain.Entities.WebsiteInfo?> Handle(GetWebsiteInfoQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id);
    }
}
