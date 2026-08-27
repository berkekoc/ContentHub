using ContentHub.BuildingBlocks.Domain;
using FluentValidation;

namespace ContentHub.BuildingBlocks.Infrastructure.Errors;

/// <summary>İstisnaları tek noktadan ProblemDefinition'a çevirir.</summary>
public static class ExceptionProblemMapper
{
    public static ProblemDefinition Map(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validation:
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                return new ProblemDefinition(
                    Status: StatusCodes.BadRequest,
                    Title: "Doğrulama başarısız.",
                    Detail: "Bir veya daha fazla doğrulama hatası oluştu.",
                    Errors: errors);

            case DomainException domain:
                return new ProblemDefinition(
                    Status: StatusCodes.UnprocessableEntity,
                    Title: "İş kuralı ihlali.",
                    Detail: domain.Message);

            case KeyNotFoundException notFound:
                return new ProblemDefinition(
                    Status: StatusCodes.NotFound,
                    Title: "Kayıt bulunamadı.",
                    Detail: notFound.Message);

            case OperationCanceledException:
                return new ProblemDefinition(
                    Status: StatusCodes.ClientClosedRequest,
                    Title: "İstek iptal edildi.");

            default:
                return new ProblemDefinition(
                    Status: StatusCodes.InternalServerError,
                    Title: "Beklenmeyen bir hata oluştu.");
        }
    }

    // AspNetCore'a bağımlı olmamak için minimal durum kodu sabitleri.
    private static class StatusCodes
    {
        public const int BadRequest = 400;
        public const int NotFound = 404;
        public const int ClientClosedRequest = 499;
        public const int UnprocessableEntity = 422;
        public const int InternalServerError = 500;
    }
}
