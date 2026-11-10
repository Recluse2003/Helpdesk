using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.ForgotPassword;
using Helpdesk.Api.Features.Auth.Login;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Auth.ForgotPassword
{
    public class ForgotPasswordCommandValidatorTests
    {
        private readonly ForgotPasswordCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveErrors()
        {
            var command = new ForgotPasswordCommand(
                "test@test.com");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyEmail_ShouldHaveError()
        {
            var command = new ForgotPasswordCommand(
                string.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WithInvalidEmail_ShouldHaveError()
        {
            var command = new ForgotPasswordCommand(
                "not-an-email");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WithLongEmail_ShouldHaveError()
        {
            var command = new ForgotPasswordCommand(
                new string('a',250) + "test@test.com");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }
    }
}
