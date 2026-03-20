namespace VRProject.Application.Common.Services
{
    public interface IUseCase<in TRequest, out TResponse>
    {
        TResponse Execute(TRequest request);
    }

    public interface IUseCase<in TRequest>
    {
        Result Execute(TRequest request);
    }
}
