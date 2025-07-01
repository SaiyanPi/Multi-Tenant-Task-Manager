using FluentValidation;
using FluentValidation.Results;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.Validators;

public class UpdateTaskItemStatusDtoValidator : AbstractValidator<UpdateTaskItemStatusDto>
{
   public UpdateTaskItemStatusDtoValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .WithMessage("NewStatus is required.");
    }
    public ValidationResult ValidateWithContext(UpdateTaskItemStatusDto dto, TaskItemStatus currentStatus)
    {
        var expectedNextStatus = (TaskItemStatus)((int)currentStatus + 1);
         if (!Enum.TryParse<TaskItemStatus>(dto.NewStatus, ignoreCase: true, out var parsed))
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