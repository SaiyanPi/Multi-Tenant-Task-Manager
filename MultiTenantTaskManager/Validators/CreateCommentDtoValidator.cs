using FluentValidation;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Comment;

namespace MultiTenantTaskManager.Validators;
public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(c => c.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(1000).WithMessage("Content must not exceed 1000 characters.");

        RuleFor(x => new { x.TaskItemId, x.ProjectId })
            .Must(x => (x.TaskItemId.HasValue ^ x.ProjectId.HasValue)) // XOR logic
            .WithMessage("Comment must belong to either a task or a project, but not both.");
    }
}