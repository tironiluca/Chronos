using System.Reflection;
using FluentValidation;
using MediatR;

namespace Chronos.Application.Common;

// Every request in this codebase returns Result or Result<T> (see Result.cs), so a validation
// failure can be reported as an ordinary Failure response instead of throwing -- callers (Api
// endpoints) already branch on IsSuccess, so short-circuiting here needs no change downstream.
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<string>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors.Select(e => e.ErrorMessage));
        }

        return failures.Count == 0 ? await next() : BuildFailureResponse(string.Join(" ", failures));
    }

    // TResponse is Result or Result<T> (enforced by the class constraint); Result<T>'s type
    // parameter isn't available at compile time here, so its Failure<T> factory is invoked via
    // reflection to build a correctly-typed failure response.
    private static TResponse BuildFailureResponse(string errorMessage)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(errorMessage);

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition)
            .MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, [errorMessage])!;
    }
}
