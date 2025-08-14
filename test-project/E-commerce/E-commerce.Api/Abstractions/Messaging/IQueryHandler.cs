using E_commerce.Api.Common.Handler;

namespace E_commerce.Api.Abstractions.Messaging
{
    public interface IQueryHandler<TQuery, TResponse>
       where TQuery : IQuery<TResponse>
    {
        Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
    }
}
