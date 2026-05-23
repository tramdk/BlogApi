using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;

namespace FloraCore.Application.Features.WebsiteInfo.Queries;

public class GetWebsiteInfoQueryHandler(IWebsiteInfoRepository repository) : IRequestHandler<GetWebsiteInfoQuery, FloraCore.Domain.Entities.WebsiteInfo?>
{
    private readonly IWebsiteInfoRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<FloraCore.Domain.Entities.WebsiteInfo?> Handle(GetWebsiteInfoQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id);
    }
}
