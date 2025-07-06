using FluentValidation;
using FluentValidation.Results;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.Validators;

public class UpdateProjectStatusDtoValidator : AbstractValidator<UpdateProjectStatusDto>
{
   public UpdateProjectStatusDtoValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .WithMessage("NewStatus is required.");
    }
    
    // custom
    public ValidationResult ValidateWithContext(UpdateProjectStatusDto dto, ProjectStatus currentStatus)
    {
        var expectedNextStatus = (ProjectStatus)((int)currentStatus + 1);
        if (!Enum.TryParse<ProjectStatus>(dto.NewStatus, ignoreCase: true, out var parsed))
        {
            return new ValidationResult(new[]
            {
                new ValidationFailure("NewStatus", $"Invalid status. The next status should be '{expectedNextStatus}'")
            });
        }

        if (parsed != expectedNextStatus)
        {
            return new ValidationResult(new[]
            {
                new ValidationFailure("NewStatus", $"The next status should be '{expectedNextStatus}'")
            });
        }

        return this.Validate(dto); // return base validation results
    }
}