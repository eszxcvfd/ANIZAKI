using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.Library;
using Anizaki.Application.Features.Library.Contracts;
using Anizaki.Application.Features.SystemStatus;
using Anizaki.Application.Features.SystemStatus.Contracts;
using Anizaki.Application.Features.Users;
using Anizaki.Application.Features.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Anizaki.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRequestValidator<RegisterUserCommand>, RegisterUserCommandValidator>();
        services.AddScoped<IRequestHandler<RegisterUserCommand, RegisterUserResponse>, RegisterUserHandler>();
        services.AddScoped<IRequestValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IRequestHandler<LoginCommand, LoginResponse>, LoginHandler>();
        services.AddScoped<IRequestValidator<LogoutCommand>, LogoutCommandValidator>();
        services.AddScoped<IRequestHandler<LogoutCommand, LogoutResponse>, LogoutHandler>();
        services.AddScoped<IRequestValidator<GetMySessionsQuery>, GetMySessionsQueryValidator>();
        services.AddScoped<IRequestHandler<GetMySessionsQuery, GetMySessionsResponse>, GetMySessionsHandler>();
        services.AddScoped<IRequestValidator<RevokeMySessionCommand>, RevokeMySessionCommandValidator>();
        services.AddScoped<IRequestHandler<RevokeMySessionCommand, RevokeMySessionResponse>, RevokeMySessionHandler>();
        services.AddScoped<IRequestValidator<MarkSessionSuspiciousCommand>, MarkSessionSuspiciousCommandValidator>();
        services.AddScoped<IRequestHandler<MarkSessionSuspiciousCommand, MarkSessionSuspiciousResponse>, MarkSessionSuspiciousHandler>();
        services.AddScoped<IRequestValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddScoped<IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>, ForgotPasswordHandler>();
        services.AddScoped<IRequestValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();
        services.AddScoped<IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>, ResetPasswordHandler>();
        services.AddScoped<IRequestValidator<VerifyEmailCommand>, VerifyEmailCommandValidator>();
        services.AddScoped<IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>, VerifyEmailHandler>();
        services.AddScoped<IRequestValidator<GetMyProfileQuery>, GetMyProfileQueryValidator>();
        services.AddScoped<IRequestHandler<GetMyProfileQuery, MyProfileResponse>, GetMyProfileHandler>();
        services.AddScoped<IRequestValidator<UpdateMyProfileCommand>, UpdateMyProfileCommandValidator>();
        services.AddScoped<IRequestHandler<UpdateMyProfileCommand, MyProfileResponse>, UpdateMyProfileHandler>();
        services.AddScoped<IRequestValidator<UpdateUserRoleCommand>, UpdateUserRoleCommandValidator>();
        services.AddScoped<IRequestHandler<UpdateUserRoleCommand, UpdateUserRoleResponse>, UpdateUserRoleHandler>();

        services.AddScoped<IRequestValidator<GetSystemStatusQuery>, GetSystemStatusQueryValidator>();
        services.AddScoped<IRequestHandler<GetSystemStatusQuery, SystemStatusResponse>, GetSystemStatusHandler>();

        // Library feature
        services.AddScoped<IRequestValidator<GetCategoriesQuery>, GetCategoriesQueryValidator>();
        services.AddScoped<IRequestHandler<GetCategoriesQuery, GetCategoriesResponse>, GetCategoriesHandler>();
        services.AddScoped<IRequestValidator<GetDrawingListQuery>, GetDrawingListQueryValidator>();
        services.AddScoped<IRequestHandler<GetDrawingListQuery, GetDrawingListResponse>, GetDrawingListHandler>();
        services.AddScoped<IRequestValidator<GetDrawingDetailQuery>, GetDrawingDetailQueryValidator>();
        services.AddScoped<IRequestHandler<GetDrawingDetailQuery, GetDrawingDetailResponse>, GetDrawingDetailHandler>();
        services.AddScoped<IRequestValidator<GetDrawingPreviewQuery>, GetDrawingPreviewQueryValidator>();
        services.AddScoped<IRequestHandler<GetDrawingPreviewQuery, GetDrawingPreviewResponse>, GetDrawingPreviewHandler>();

        return services;
    }
}
