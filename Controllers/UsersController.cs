using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using ProgrammingQuotesApi.Models;
using ProgrammingQuotesApi.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProgrammingQuotesApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IQuoteService _quoteService;

        public UsersController(IUserService userService, IQuoteService quoteService)
        {
            _userService = userService;
            _quoteService = quoteService;
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Create([FromBody] UserRegister req)
        {
            if (await _userService.UsernameTakenAsync(req.Username))
                return BadRequest(new { message = "Username " + req.Username + " is already taken" });

            await _userService.RegisterAsync(req);
            return Ok(new { message = "Registration successful" });
        }

        /// <summary>
        /// Authenticates an existing user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///        "username": "admin",
        ///        "password": "admin"
        ///     }
        ///
        /// </remarks>
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Authenticate([FromBody] UserAuthReq req)
        {
            UserAuthRes user = await _userService.AuthenticateAsync(req.Username, req.Password);

            if (user == null)
                return Unauthorized(new { message = "User or password invalid" });

            return Ok(user);
        }

        /// <summary>
        /// Returns all users
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<User>> GetAll()
        {
            IEnumerable<User> users = _userService.GetAll();
            return Ok(users);
        }

        /// <summary>
        /// Returns a user data for a given username
        /// </summary>
        [HttpGet("{username}")]
        public async Task<ActionResult<User>> GetByUsername(string username)
        {
            User user = await _userService.GetByUsernameAsync(username);

            return user == null ? NotFound() : Ok(user);
        }

        /// <summary>
        /// Admin dashboard 🔒
        /// </summary>
        [HttpGet]
        [Route("dashboard")]
        [Authorize(Roles = "Admin,Editor")]
        public string Dashboard() => "Welcome to the Dashboard for admins and editors only.";

        /// <summary>
        /// Returns my user details 🔒
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("me")]
        public async Task<ActionResult<User>> GetMyUser()
        {
            User user = await _userService.GetByUsernameAsync(User.Identity.Name);
            return Ok(user);
        }

        /// <summary>
        /// Replace my old user data with a new one 🔒
        /// </summary>
        [HttpPut]
        [Authorize]
        [Route("me")]
        public async Task<ActionResult> Update([FromBody] UserUpdate req)
        {
            User myUser = await _userService.GetByUsernameAsync(User.Identity.Name);
            bool usernameTaken = await _userService.UsernameTakenAsync(req.Username);
            if ((req.Username != myUser.Username) && usernameTaken)
                return BadRequest(new { message = "Username " + req.Username + " is already taken" });

            await _userService.UpdateAsync(myUser, req);
            return Ok(new { message = "User updated successfully" });
        }

        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///       {
        ///          "path": "/firstName",
        ///          "op": "replace",
        ///          "value": "Master"
        ///       }
        ///     ]
        ///
        /// </remarks>
        /// <summary>
        /// Update certain properties of my user 🔒
        /// </summary>
        [HttpPatch]
        [Authorize]
        [Route("me")]
        public async Task<ActionResult> Patch(JsonPatchDocument<User> patch)
        {
            User user = await _userService.GetByUsernameAsync(User.Identity.Name);
            if (user == null) return NotFound();

            patch.ApplyTo(user);
            await _userService.UpdateAsync(user);

            return Ok(user);
        }

        /// <summary>
        /// Delete a user by id 🔒
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            User user = await _userService.GetByIdAsync(id);

            if (user == null)
                return NotFound();

            await _userService.DeleteAsync(user);

            return NoContent();
        }

        /// <summary>
        /// Add favorite quote 🔒
        /// </summary>
        /// <remarks>
        /// For example: "5a6ce86e2af929789500e7e4"
        /// </remarks>
        [HttpPost]
        [Authorize]
        [Route("addFavorite")]
        public async Task<ActionResult<User>> addFavorite([FromBody] string quoteId)
        {
            Quote quote = await _quoteService.GetByIdAsync(quoteId);
            if (quote == null) return NotFound();

            User user = await _userService.GetByUsernameAsync(User.Identity.Name);
            if (user == null) return NotFound();

            await _userService.AddFavoriteQuoteAsync(user, quote);

            return Ok(user);
        }
    }
}
