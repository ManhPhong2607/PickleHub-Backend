using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleHub.Authen.Application.Features.Auth.BlockUser;
using PickleHub.Authen.Application.Features.Auth.ChangePassword;
using PickleHub.Authen.Application.Features.Auth.ForgotPassword;
using PickleHub.Authen.Application.Features.Auth.Login;
using PickleHub.Authen.Application.Features.Auth.Logout;
using PickleHub.Authen.Application.Features.Auth.RefreshTokens;
using PickleHub.Authen.Application.Features.Auth.Register;
using PickleHub.Authen.Application.Features.Auth.ResendVerification;
using PickleHub.Authen.Application.Features.Auth.ResetPassword;
using PickleHub.Authen.Application.Features.Auth.VerifyEmail;
using PickleHub.Common.Exceptions;

namespace PickleHub.Authen.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController(ISender mediator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return Created(string.Empty, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken ct)
        {
            await mediator.Send(command, ct);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
        {
            await mediator.Send(command, ct);  
            return Ok(new { message = "Nếu email tồn tại, link đặt lại mật khẩu đã được gửi." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
        {
            await mediator.Send(command, ct);
            return Ok(new { message = "Đặt lại mật khẩu thành công." });
        }

        [Authorize]
        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangePassword(
                    [FromBody] ChangePasswordCommand command, CancellationToken ct)
        {       
            await mediator.Send(command, ct);
            return NoContent();
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
        {
            var result = await mediator.Send(new VerifyEmailCommand(token), ct);
            return Ok(result);
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationCommand command, CancellationToken ct)
        {
            await mediator.Send(command, ct);
            return Ok(new { message = "Nếu email tồn tại và chưa xác minh, link xác minh đã được gửi lại." });
        }

    }

}

