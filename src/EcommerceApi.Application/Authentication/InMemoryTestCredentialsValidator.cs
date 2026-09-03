namespace EcommerceApi.Application.Authentication;

public sealed class InMemoryTestCredentialsValidator : ITestCredentialsValidator
{
    private const string EvaluatorEmail = "dev@martech.com";
    private const string EvaluatorPassword = "Senha@123";

    public bool AreValid(string email, string password) =>
        string.Equals(email, EvaluatorEmail, StringComparison.Ordinal) &&
        string.Equals(password, EvaluatorPassword, StringComparison.Ordinal);
}
