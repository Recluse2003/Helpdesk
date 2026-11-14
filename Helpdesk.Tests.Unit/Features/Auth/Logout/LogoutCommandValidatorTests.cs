using FluentValidation.TestHelper;
using Helpdesk.Api.Features.Auth.Logout;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helpdesk.Tests.Unit.Features.Auth.Logout
{
    public class LogoutCommandValidatorTests
    {
        private readonly LogoutCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldNotHaveError()
        {
            var command = new LogoutCommand("refresh-token");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
