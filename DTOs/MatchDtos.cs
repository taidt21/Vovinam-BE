namespace VovinamApi.DTOs;

public class MatchDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? AthleteRedId { get; set; }
    public Guid? AthleteBlueId { get; set; }
    public Guid? NextMatchId { get; set; }
    public string? NextMatchSlot { get; set; }
    public string Vong { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "cho_thi";
    public string? LyDoKetThuc { get; set; }
    public Guid? NguoiThangId { get; set; }
    public string? CourtId { get; set; }
}
// Dùng khi sửa ĐÚNG 1 trận đang diễn ra (bắt đầu, kết thúc, đổi sân, sửa
// kết quả) — khác MatchUpsertDto (ghi đè NGUYÊN cả nội dung, chỉ dùng lúc
// bốc thăm). Không có Id/EventId/NextMatchId/NextMatchSlot/Vong vì những
// trường đó không đổi sau khi bốc thăm xong.
public class MatchUpdateDto
{
    public Guid? AthleteRedId { get; set; }
    public Guid? AthleteBlueId { get; set; }
    public string TrangThai { get; set; } = "cho_thi";
    public string? LyDoKetThuc { get; set; }
    public Guid? NguoiThangId { get; set; }
    public string? CourtId { get; set; }
}
public class MatchUpsertDto
{
    public Guid Id { get; set; } // sinh sẵn ở frontend lúc bốc thăm, giữ nguyên để nextMatchId trỏ đúng
    public Guid? AthleteRedId { get; set; }
    public Guid? AthleteBlueId { get; set; }
    public Guid? NextMatchId { get; set; }
    public string? NextMatchSlot { get; set; }
    public string Vong { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "cho_thi";
    public string? LyDoKetThuc { get; set; }
    public Guid? NguoiThangId { get; set; }
    public string? CourtId { get; set; }
}