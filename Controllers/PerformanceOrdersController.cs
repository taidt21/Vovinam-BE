using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VovinamApi.Data;
using VovinamApi.DTOs;
using VovinamApi.Models;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/performance-orders")]
public class PerformanceOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PerformanceOrdersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<PerformanceOrderDto>>> GetAll()
    {
        var orders = await _db.PerformanceOrders.ToListAsync();
        return Ok(orders.Select(ToDto));
    }
    [Authorize(Roles = "Admin")]
    [HttpPut("by-event/{eventId}")]
    public async Task<ActionResult<List<PerformanceOrderDto>>> ReplaceForEvent(Guid eventId, List<PerformanceOrderUpsertDto> orders)
    {
        var old = await _db.PerformanceOrders.Where(o => o.EventId == eventId).ToListAsync();
        _db.PerformanceOrders.RemoveRange(old);

        var created = orders.Select(o => new PerformanceOrder
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AthleteId = o.AthleteId,
            TeamId = o.TeamId,
            ThuTu = o.ThuTu,
        }).ToList();
        _db.PerformanceOrders.AddRange(created);

        await _db.SaveChangesAsync();
        return Ok(created.Select(ToDto));
    }

    private static PerformanceOrderDto ToDto(PerformanceOrder o) => new()
    {
        Id = o.Id,
        EventId = o.EventId,
        AthleteId = o.AthleteId,
        TeamId = o.TeamId,
        ThuTu = o.ThuTu,
    };
}