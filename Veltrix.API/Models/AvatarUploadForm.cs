using FluentValidation;
using Veltrix.Application.Common.Files;

namespace Veltrix.API.Models;

/// <summary>
/// multipart/form-data body for POST /api/profile/avatar.
/// Lives in the API layer because IFormFile is an ASP.NET Core type.
/// </summary>
public sealed class AvatarUploadForm
{
    /// <summary>JPG, PNG or WEBP image, max 2 MB.</summary>
    public IFormFile? File { get; set; }
}

public sealed class AvatarUploadFormValidator : AbstractValidator<AvatarUploadForm>
{
    public AvatarUploadFormValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Choose an image file.")
            .DependentRules(() =>
            {
                RuleFor(x => x.File!.Length)
                    .GreaterThan(0).WithMessage("The file is empty.")
                    .LessThanOrEqualTo(AvatarRules.MaxBytes).WithMessage("The image must be 2 MB or smaller.")
                    .OverridePropertyName("file");

                RuleFor(x => x.File!.FileName)
                    .Must(name => AvatarRules.AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
                    .WithMessage("Only .jpg, .jpeg, .png and .webp files are allowed.")
                    .OverridePropertyName("file");

                RuleFor(x => x.File!.ContentType)
                    .Must(type => AvatarRules.AllowedContentTypes.Contains(type.ToLowerInvariant()))
                    .WithMessage("Only JPG, PNG and WEBP images are allowed.")
                    .OverridePropertyName("file");
            });
    }
}
