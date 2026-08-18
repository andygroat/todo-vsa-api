using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Todo.VSA.Api.Infrastructure.Behaviours;

namespace Todo.VSA.Api.Tests.Infrastructure.Behaviours;

/// <summary>
/// Tests for <see cref="ValidationBehavior{TRequest, TResponse}"/>.
/// </summary>
public class ValidationBehaviorTests
{
    private sealed record SampleRequest(string Value) : IRequest<string>;

    /// <summary>
    /// Validator requiring <c>Value</c> to be non-empty.
    /// </summary>
    private sealed class NonEmptyValueValidator : AbstractValidator<SampleRequest>
    {
        public NonEmptyValueValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    /// <summary>
    /// Validator requiring <c>Value</c> to have a minimum length of 3.
    /// </summary>
    private sealed class MinLengthValidator : AbstractValidator<SampleRequest>
    {
        public MinLengthValidator()
        {
            RuleFor(x => x.Value).MinimumLength(3);
        }
    }

    [Test]
    public async Task Handle_WhenNoValidators_InvokesNextAndReturnsResponse()
    {
        // Arrange
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        // Act
        var result = await behavior.Handle(new SampleRequest("hi"), next, CancellationToken.None);

        // Assert
        await Assert.That(nextCalled).IsTrue();
        await Assert.That(result).IsEqualTo("ok");
    }

    [Test]
    public async Task Handle_WhenAllValidatorsPass_InvokesNextAndReturnsResponse()
    {
        // Arrange
        var validators = new IValidator<SampleRequest>[]
        {
            new NonEmptyValueValidator(),
            new MinLengthValidator()
        };
        var behavior = new ValidationBehavior<SampleRequest, string>(validators);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        // Act
        var result = await behavior.Handle(new SampleRequest("hello"), next, CancellationToken.None);

        // Assert
        await Assert.That(result).IsEqualTo("ok");
    }

    [Test]
    public async Task Handle_WhenValidationFails_ThrowsValidationExceptionAndDoesNotCallNext()
    {
        // Arrange
        var validators = new IValidator<SampleRequest>[] { new NonEmptyValueValidator() };
        var behavior = new ValidationBehavior<SampleRequest, string>(validators);

        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        // Act & Assert
        var ex = await Assert.That(async () =>
            await behavior.Handle(new SampleRequest(string.Empty), next, CancellationToken.None))
            .Throws<ValidationException>();

        await Assert.That(nextCalled).IsFalse();
    }

    [Test]
    public async Task Handle_WhenMultipleValidatorsFail_AggregatesAllFailures()
    {
        // Arrange
        var validators = new IValidator<SampleRequest>[]
        {
            new NonEmptyValueValidator(),
            new MinLengthValidator()
        };
        var behavior = new ValidationBehavior<SampleRequest, string>(validators);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        // Act
        ValidationException? caught = null;
        try
        {
            await behavior.Handle(new SampleRequest(string.Empty), next, CancellationToken.None);
        }
        catch (ValidationException ex)
        {
            caught = ex;
        }

        // Assert - failures from both validators must be present
        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Errors.Count()).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Handle_WhenOneOfMultipleValidatorsFails_Throws()
    {
        // Arrange - first passes, second fails
        var passing = new PassingValidator();
        var failing = new FailingValidator();
        var behavior = new ValidationBehavior<SampleRequest, string>(new IValidator<SampleRequest>[] { passing, failing });
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        // Act & Assert
        await Assert.That(async () =>
            await behavior.Handle(new SampleRequest("value"), next, CancellationToken.None))
            .Throws<ValidationException>();
    }

    [Test]
    public async Task Handle_PassesCancellationTokenToNext_WhenNoValidators()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        using var cts = new CancellationTokenSource();

        CancellationToken received = default;
        RequestHandlerDelegate<string> next = ct =>
        {
            received = ct;
            return Task.FromResult("ok");
        };

        await behavior.Handle(new SampleRequest("x"), next, cts.Token);

        await Assert.That(received).IsEqualTo(cts.Token);
    }

    /// <summary>
    /// Test validator that always returns a successful validation result.
    /// </summary>
    private sealed class PassingValidator : AbstractValidator<SampleRequest>;

    /// <summary>
    /// Test validator that always returns a failing validation result, regardless of the request.
    /// Used to exercise multi-validator aggregation deterministically.
    /// </summary>
    private sealed class FailingValidator : IValidator<SampleRequest>
    {
        public ValidationResult Validate(IValidationContext context) =>
            new(new[] { new ValidationFailure("Value", "forced failure") });

        public ValidationResult Validate(SampleRequest instance) =>
            new(new[] { new ValidationFailure("Value", "forced failure") });

        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) =>
            Task.FromResult(new ValidationResult(new[] { new ValidationFailure("Value", "forced failure") }));

        public Task<ValidationResult> ValidateAsync(SampleRequest instance, CancellationToken cancellation = default) =>
            Task.FromResult(new ValidationResult(new[] { new ValidationFailure("Value", "forced failure") }));

        public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();
        public bool CanValidateInstancesOfType(Type type) => type == typeof(SampleRequest);
    }
}
