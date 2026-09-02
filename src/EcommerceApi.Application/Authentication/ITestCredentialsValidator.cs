namespace EcommerceApi.Application.Authentication;

public interface ITestCredentialsValidator
{
    bool AreValid(string email, string password);
}
