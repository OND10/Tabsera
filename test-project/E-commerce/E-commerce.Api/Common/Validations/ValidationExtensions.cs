using E_commerce.Api.Common.Exceptions;

namespace E_commerce.Api.Common.Validations
{
    public static class ValidationExtensions
    {
        public static void ThrowIfNullOrEmpty(this string? value, string field) 
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                throw new AppException($"{field} is required");
            }
        }
    }
}
