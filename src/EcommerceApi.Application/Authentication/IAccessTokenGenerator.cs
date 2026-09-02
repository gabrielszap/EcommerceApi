namespace EcommerceApi.Application.Authentication;

public interface IAccessTokenGenerator
{
    AccessToken Generate(string email);
}
