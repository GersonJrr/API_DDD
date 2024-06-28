using Entities.Entities;
using Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using WebApis.Models;
using WebApis.Token;
using WebAPIs.Token;

namespace WebApis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly SignInManager<ApplicationUser> _signInManager;

        public UsersController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser>signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        [Produces("Application/json")]
        [HttpPost("/api/CriarToken")]
        public async Task<IActionResult> CriarToken([FromBody] Login login)
        {
            if (string.IsNullOrWhiteSpace(login.email) || string.IsNullOrWhiteSpace(login.senha))
            {
                return Unauthorized();
            }

            var resultado = await
                _signInManager.PasswordSignInAsync(login.email, login.senha, false, lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
               var userCurrent = await _userManager.FindByEmailAsync(login.email); 
                var idUsuario = userCurrent.Id;

                var token = new TokenJWTBuilder()
                    .AddSecurityKey(JwtSecurityKey.Create("Secret_Key-12345678"))
                    .AddSubject("Test.Security.Bearer")
                    .AddIssuer("Test.Security.Bearer")
                    .AddAudience("Test.Security.Bearer")
                    .AddClaim("idUsuario", idUsuario)
                    .AddExpiry(5)
                    .Builder();

                return Ok(token.value);

            }
            else
            {
                return Unauthorized();
            }
            
        }

        [AllowAnonymous]
        [Produces("Application/json")]
        [HttpPost("/api/AdicionarUsuario")]
        public async Task<IActionResult> AdicionarUsuario([FromBody] Login login)
        {
            if (string.IsNullOrWhiteSpace(login.email) || string.IsNullOrWhiteSpace(login.senha))
            {
                return Ok("Falta alguns dados");
            }

            var user = new ApplicationUser
            {
                UserName = login.email,
                Email = login.email,
                CPF = login.cpf,
                Tipo = TipoUsuario.Comum,
            };

            var resultado = await _userManager.CreateAsync(user, login.senha);
            if (resultado.Errors.Any())
            {
                return Ok(resultado.Errors);
            }

            //Geracao de confirmação
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            //retorno email
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var resultado2 = await _userManager.ConfirmEmailAsync(user, code);

            if (resultado2.Succeeded)
                return Ok("Usuário adicionado com sucesso");
            else
                return Ok("Erro ao confirmar usuário");
        }

    }
}
