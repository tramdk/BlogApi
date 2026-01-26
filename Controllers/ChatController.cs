using BlogApi.Application.Features.Chat.Commands.SendMessage;
using BlogApi.Application.Features.Chat.Queries.GetChatHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send")]
    public async Task<ActionResult<Guid>> SendMessage(SendMessageCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    [HttpGet("history/{otherUserId}")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetChatHistory(Guid otherUserId)
    {
        return Ok(await _mediator.Send(new GetChatHistoryQuery(otherUserId)));
    }
}
