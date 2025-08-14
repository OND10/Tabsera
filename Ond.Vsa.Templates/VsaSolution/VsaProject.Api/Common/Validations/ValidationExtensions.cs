using VsaProject.Api.Common.Exceptions;

namespace VsaProject.Api.Common.Validations
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
