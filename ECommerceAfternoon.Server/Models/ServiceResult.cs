namespace ECommerceAfternoon.Server.Models
{
    public enum ServiceResultStatus
    {
        Success,
        BadRequest,
        NotFound
    }

    public class ServiceResult
    {
        public ServiceResultStatus Status { get; private set; }

        public string? Message { get; private set; }

        public bool IsSuccess => Status == ServiceResultStatus.Success;

        public static ServiceResult Success()
            => new() { Status = ServiceResultStatus.Success };

        public static ServiceResult BadRequest(string message)
            => new() { Status = ServiceResultStatus.BadRequest, Message = message };

        public static ServiceResult NotFound(string message)
            => new() { Status = ServiceResultStatus.NotFound, Message = message };
    }
}
