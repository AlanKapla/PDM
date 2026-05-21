using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.Extensions
{
    public static class CommonValidationExtensions
    {
        public static IRuleBuilderOptions<T, Guid> RequiredId<T>(
            this IRuleBuilder<T, Guid> ruleBuilder)
            => ruleBuilder
                .NotEmpty()
                .WithMessage("'{PropertyName}' is required.");

        public static IRuleBuilderOptions<T, Guid> NotCurrentUser<T>(
            this IRuleBuilder<T, Guid> ruleBuilder,
            ICurrentUser currentUser)
            => ruleBuilder
                .Must(id => id != currentUser.Id)
                .WithMessage("Cannot perform this action on yourself.");

        public static IRuleBuilderOptions<T, List<Guid>> NotCurrentUser<T>(
            this IRuleBuilder<T, List<Guid>> ruleBuilder,
            ICurrentUser currentUser)
            => ruleBuilder
                .Must(ids => ids is null || !ids.Contains(currentUser.Id))
                .WithMessage("Cannot perform this action on yourself.");

        public static IRuleBuilderOptions<T, int> NonNegativeOrder<T>(
            this IRuleBuilder<T, int> ruleBuilder)
            => ruleBuilder
                .GreaterThanOrEqualTo(0)
                .WithMessage("'{PropertyName}' must be non-negative.");

        public static IRuleBuilderOptions<T, int> PageSize<T>(
            this IRuleBuilder<T, int> ruleBuilder,
            int max = 100)
            => ruleBuilder
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(max).WithMessage($"Page size cannot exceed {max}");

        public static IRuleBuilderOptions<T, int> NonNegativeOffset<T>(
            this IRuleBuilder<T, int> ruleBuilder)
            => ruleBuilder
                .GreaterThanOrEqualTo(0).WithMessage("Offset must be non-negative");

        public static IRuleBuilderOptions<T, List<Guid>> UniqueIds<T>(
            this IRuleBuilder<T, List<Guid>> ruleBuilder)
            => ruleBuilder
                .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
                .WithMessage("'{PropertyName}' must contain unique IDs.");

        public static IRuleBuilderOptions<T, string> ValidColorRgb<T>(
            this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .WithMessage("'{PropertyName}' must be a valid RGB hex color (e.g. #FF5733).");

        /// <summary>
        /// Validates that the file name has an extension contained in <paramref name="allowedExtensions"/>.
        /// Comparison is case-insensitive. Extensions in the collection are expected to start with '.'.
        /// </summary>
        public static IRuleBuilderOptions<T, string> AllowedFileExtension<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            IReadOnlyCollection<string> allowedExtensions)
            => ruleBuilder
                .Must(fileName =>
                {
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        return false;
                    }

                    string extension = Path.GetExtension(fileName).ToLowerInvariant();
                    return allowedExtensions.Contains(extension);
                })
                .WithMessage(_ => $"Allowed file formats are: {string.Join(", ", allowedExtensions)}");

        /// <summary>
        /// Validates that the content type (MIME) is contained in <paramref name="allowedContentTypes"/>.
        /// Comparison is case-insensitive.
        /// </summary>
        public static IRuleBuilderOptions<T, string> AllowedContentType<T>(
            this IRuleBuilder<T, string> ruleBuilder,
            IReadOnlyCollection<string> allowedContentTypes)
            => ruleBuilder
                .Must(contentType =>
                {
                    if (string.IsNullOrWhiteSpace(contentType))
                    {
                        return false;
                    }

                    return allowedContentTypes.Contains(contentType.ToLowerInvariant());
                })
                .WithMessage(_ => $"Allowed MIME types are: {string.Join(", ", allowedContentTypes)}");

        /// <summary>
        /// Validates that the file size in bytes is greater than 0 and not greater than <paramref name="maxBytes"/>.
        /// </summary>
        public static IRuleBuilderOptions<T, long> MaxFileSize<T>(
            this IRuleBuilder<T, long> ruleBuilder,
            long maxBytes)
            => ruleBuilder
                .GreaterThan(0).WithMessage("File cannot be empty.")
                .LessThanOrEqualTo(maxBytes)
                .WithMessage($"File cannot be larger than {maxBytes / 1024 / 1024} MB.");

        /// <summary>
        /// Validates that the provided <see cref="ResourceScope"/> value is a defined enum member.
        /// </summary>
        public static IRuleBuilderOptions<T, ResourceScope> ValidScope<T>(
            this IRuleBuilder<T, ResourceScope> ruleBuilder)
            => ruleBuilder
                .Must(scope => Enum.IsDefined(typeof(ResourceScope), scope))
                .WithMessage("'{PropertyName}' must be a valid resource scope.");
    }
}
