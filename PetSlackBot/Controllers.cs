
using Microsoft.AspNetCore.Mvc;

namespace PetSlackBot;

[ApiController]
[Route("slack")]
public class SlackController : ControllerBase
{
    [HttpPost("commands")]
    public IActionResult HandleCommand()
    {
        var form = Request.Form;

        var userId = form["user_id"];
        var command = form["command"];

        return Ok($"Бот жив 🐾 user: {userId}, command: {command}");
    }
}