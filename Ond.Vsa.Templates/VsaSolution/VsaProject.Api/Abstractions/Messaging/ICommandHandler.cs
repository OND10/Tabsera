using VsaProject.Api.Common.Handler;

namespace VsaProject.Api.Abstractions.Messaging
{
    public interface ICommandHandler<TCommand, TResponse>
       where TCommand : ICommand<TResponse>
    {
        Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
