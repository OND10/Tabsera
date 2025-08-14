using E_commerce.Api.Common.Handler;

namespace E_commerce.Api.Abstractions.Messaging
{
    public interface ICommandHandler<TCommand, TResponse>
       where TCommand : ICommand<TResponse>
    {
        Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
