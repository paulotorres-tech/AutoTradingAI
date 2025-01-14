//using Azure.Messaging.ServiceBus;
using AutoTradingAI.User.Core.Interfaces;
using AutoTradingAI.User.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using AutoTradingAI.User.Api.Models;
using Azure.Messaging.ServiceBus;

namespace AutoTradingAI.User.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        private readonly IAuthService _authService;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly string _topicName = "user-registered";

        public AuthController(ILogger<AuthController> logger, IConfiguration configuration, IAuthService authService, ServiceBusClient serviceBusClient)
        {
            _configuration = configuration;
            _logger = logger;

            _authService = authService;
            _serviceBusClient = serviceBusClient;
            _topicName = _configuration["ServiceBus:TopicName"];
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // register user on database
                var user = await _authService.RegisterAsync(request.Username,
                    request.Email,
                    request.Password);

                var sender = _serviceBusClient.CreateSender(_topicName);
                var userRegisteredEvent = new
                {
                    Event = "UserRegistered",
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                };
                var message = new ServiceBusMessage(JsonConvert.SerializeObject(userRegisteredEvent))
                {
                    ContentType = "application/json",
                };

                await sender.SendMessageAsync(message);

                return Ok(new { user.Id, user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed. " + 
                    $"[Username: {request.Username} | Email: {request.Email}]");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var token = await _authService.LoginAsync(request.Email, request.Password);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed. " +
                    $"[Email: {request.Email}]");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
