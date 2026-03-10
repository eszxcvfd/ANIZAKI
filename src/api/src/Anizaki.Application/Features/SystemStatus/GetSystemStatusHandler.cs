using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.SystemStatus.Contracts;
using Anizaki.Domain.ValueObjects;

namespace Anizaki.Application.Features.SystemStatus;

public sealed class GetSystemStatusHandler : IRequestHandler<GetSystemStatusQuery, SystemStatusResponse>
{
    private readonly ISystemStatusProbe _systemStatusProbe;
    private readonly IRequestValidator<GetSystemStatusQuery> _validator;

    public GetSystemStatusHandler(
        ISystemStatusProbe systemStatusProbe,
        IRequestValidator<GetSystemStatusQuery> validator)
    {
        _systemStatusProbe = systemStatusProbe;
        _validator = validator;
    }

    public async Task<SystemStatusResponse> HandleAsync(
        GetSystemStatusQuery request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var probeResponse = await _systemStatusProbe.ProbeAsync(request.CorrelationId, cancellationToken);
        var normalizedStatus = ServiceHealth.FromExternal(probeResponse.Status);
        return probeResponse with
        {
            Status = normalizedStatus.Value
        };
    }
}
