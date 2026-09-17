using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using VovinamApi.Data;
using VovinamApi.Models;

namespace VovinamApi.Services;

public class ExcelRegistrationImportService
{
    private const int MaxWarnings = 200;

    private readonly ApplicationDbContext _db;
    private readonly AthleteImageService _imageService;
    private readonly List<string> _warnings = [];

    public ExcelRegistrationImportService(
        ApplicationDbContext db,
        AthleteImageService imageService)
    {
        _db = db;
        _imageService = imageService;
    }

    public async Task<object> ImportAsync(Stream stream)
    {
        _warnings.Clear();

        using var wb = new XLWorkbook(stream);
        var teamsSheet = GetSheet(wb, "Đơn vị", "Don vi");
        var staffSheet = GetSheet(wb, "Trưởng đoàn - HLV", "Cán bộ đoàn", "Can bo");
        var athleteSheet = GetSheet(wb, "VĐV", "VDV", "VĐV đăng ký");

        if (teamsSheet is null)
            throw new InvalidDataException("Không tìm thấy sheet \"Đơn vị\" trong file Excel.");
        if (athleteSheet is null)
            throw new InvalidDataException("Không tìm thấy sheet \"VĐV\" trong file Excel.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var teams = await ImportTeams(teamsSheet);
        var staff = await ImportStaff(staffSheet, teams);
        var athletes = await ImportAthletes(athleteSheet, teams);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            donVi = teams.TeamIds.Values.Distinct().Count(),
            donViMoi = teams.Created,
            logoDonVi = teams.Images,
            canBo = staff.Imported,
            anhCanBo = staff.Images,
            vdv = athletes.Imported,
            anhVdv = athletes.Images,
            dangKyNoiDung = athletes.Registrations,
            boQua = staff.Skipped + athletes.Skipped,
            canhBao = _warnings
        };
    }

    private static IXLWorksheet? GetSheet(XLWorkbook wb, params string[] names)
    {
        var normalizedNames = names.Select(Normalize).ToHashSet();
        return wb.Worksheets.FirstOrDefault(ws => normalizedNames.Contains(Normalize(ws.Name)));
    }

    private async Task<TeamImportResult> ImportTeams(IXLWorksheet ws)
    {
        var headers = HeaderMap.Create(ws);
        var nameColumn = headers.Require("Tên đơn vị", "Đơn vị");
        var codeColumn = headers.Find("Mã đơn vị");
        var logoColumn = headers.Find("Link logo", "Logo");

        var existingTeams = await _db.Teams.ToListAsync();
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var images = 0;

        foreach (var team in existingTeams)
            result[Normalize(team.Ten)] = team.Id;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = CellText(row, nameColumn);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var key = Normalize(name);
            var team = existingTeams.FirstOrDefault(x => Normalize(x.Ten) == key);
            if (team is null)
            {
                team = new Team { Id = Guid.NewGuid(), Ten = name };
                _db.Teams.Add(team);
                existingTeams.Add(team);
                created++;
            }

            if (logoColumn is int logoIndex)
            {
                var logoUrl = CellText(row, logoIndex);
                if (!string.IsNullOrWhiteSpace(logoUrl))
                {
                    var downloaded = await _imageService.TryDownloadAsync(logoUrl, folder: "teams");
                    if (!string.IsNullOrWhiteSpace(downloaded))
                    {
                        var oldLogo = team.LogoUrl;
                        team.LogoUrl = downloaded;
                        images++;
                        if (!string.Equals(oldLogo, downloaded, StringComparison.OrdinalIgnoreCase))
                            _imageService.DeleteLocalFile(oldLogo, "teams");
                    }
                    else
                    {
                        Warn($"Sheet {ws.Name}, dòng {row.RowNumber()}: không tải được logo của đơn vị \"{name}\".");
                    }
                }
            }

            result[key] = team.Id;

            if (codeColumn is int codeIndex)
            {
                var code = Normalize(CellText(row, codeIndex));
                if (!string.IsNullOrWhiteSpace(code)) result[code] = team.Id;
            }
        }

        return new TeamImportResult(result, created, images);
    }

    private async Task<ImportCount> ImportStaff(
        IXLWorksheet? ws,
        TeamImportResult teams)
    {
        if (ws is null) return new ImportCount(0, 0, 0, 0);

        var headers = HeaderMap.Create(ws);
        var teamColumn = headers.Require("Tên đơn vị", "Đơn vị", "Mã đơn vị");
        var nameColumn = headers.Require("Họ tên", "Họ và tên", "Tên");
        var roleColumn = headers.Require("Mã vai trò", "Vai trò");
        var imageColumn = headers.Find("Link ảnh", "Ảnh", "Ảnh đại diện", "URL ảnh");

        var existing = (await _db.CanBoDoans
                .AsNoTracking()
                .Select(x => new { x.TeamId, x.HoTen, x.VaiTro })
                .ToListAsync())
            .Select(x => StaffKey(x.TeamId, x.HoTen, x.VaiTro))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        var skipped = 0;
        var images = 0;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var rowNumber = row.RowNumber();
            var team = FindTeam(CellText(row, teamColumn), teams.TeamIds);
            var name = CellText(row, nameColumn);
            var role = NormalizeRole(CellText(row, roleColumn));

            if (team is null || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                Warn($"Sheet {ws.Name}, dòng {rowNumber}: thiếu đơn vị hoặc họ tên cán bộ.");
                continue;
            }

            if (role is null)
            {
                skipped++;
                Warn($"Sheet {ws.Name}, dòng {rowNumber}: vai trò không hợp lệ.");
                continue;
            }

            var key = StaffKey(team.Value, name, role);
            if (!existing.Add(key))
            {
                skipped++;
                Warn($"Sheet {ws.Name}, dòng {rowNumber}: cán bộ \"{name}\" đã tồn tại, bỏ qua.");
                continue;
            }

            var imageUrl = imageColumn is int imageIndex ? CellText(row, imageIndex) : "";
            var downloadedImage = await _imageService.TryDownloadAsync(imageUrl, folder: "can-bo-doan");
            if (!string.IsNullOrWhiteSpace(downloadedImage)) images++;
            else if (!string.IsNullOrWhiteSpace(imageUrl))
                Warn($"Sheet {ws.Name}, dòng {rowNumber}: không tải được ảnh cán bộ \"{name}\".");

            _db.CanBoDoans.Add(new CanBoDoan
            {
                Id = Guid.NewGuid(),
                TeamId = team.Value,
                HoTen = name,
                VaiTro = role,
                AnhDaiDien = downloadedImage
            });
            imported++;
        }

        return new ImportCount(imported, 0, skipped, images);
    }

    private async Task<ImportCount> ImportAthletes(
        IXLWorksheet ws,
        TeamImportResult teams)
    {
        var headers = HeaderMap.Create(ws);
        var teamColumn = headers.Require("Tên đơn vị", "Đơn vị", "Mã đơn vị");
        var nameColumn = headers.Require("Họ tên", "Họ và tên", "Tên");
        var birthYearColumn = headers.Require("Năm sinh");
        var genderColumn = headers.Require("Giới tính");
        var ageGroupColumn = headers.Require("Mã nhóm tuổi", "Nhóm tuổi");
        var eventsColumn = headers.Find("Nội dung", "Nội dung đăng ký");
        var imageColumn = headers.Find("Link ảnh", "Ảnh", "Ảnh đại diện", "URL ảnh");

        var existingAthletes = (await _db.Athletes
                .AsNoTracking()
                .Select(x => new { x.TeamId, x.HoTen, x.NamSinh })
                .ToListAsync())
            .Select(x => AthleteKey(x.TeamId, x.HoTen, x.NamSinh))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var events = await _db.Events.AsNoTracking().ToListAsync();
        var eventsByName = events.ToLookup(x => Normalize(x.Ten));
        var imported = 0;
        var registrations = 0;
        var skipped = 0;
        var images = 0;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var rowNumber = row.RowNumber();
            var team = FindTeam(CellText(row, teamColumn), teams.TeamIds);
            var name = CellText(row, nameColumn);
            var birthYear = ParseInt(CellText(row, birthYearColumn));
            var ageGroup = ParseInt(CellText(row, ageGroupColumn));
            var gender = ParseGender(CellText(row, genderColumn));

            if (team is null || string.IsNullOrWhiteSpace(name)
                || birthYear <= 0 || ageGroup <= 0 || gender is null)
            {
                skipped++;
                Warn($"Sheet {ws.Name}, dòng {rowNumber}: dữ liệu VĐV không hợp lệ hoặc không tìm thấy đơn vị.");
                continue;
            }

            var key = AthleteKey(team.Value, name, birthYear);
            if (!existingAthletes.Add(key))
            {
                skipped++;
                Warn($"Sheet {ws.Name}, dòng {rowNumber}: VĐV \"{name}\" ({birthYear}) đã tồn tại, bỏ qua.");
                continue;
            }

            var imageUrl = imageColumn is int imageIndex ? CellText(row, imageIndex) : "";
            var downloadedImage = await _imageService.TryDownloadAsync(imageUrl, folder: "athletes");
            if (!string.IsNullOrWhiteSpace(downloadedImage)) images++;
            else if (!string.IsNullOrWhiteSpace(imageUrl))
                Warn($"Sheet {ws.Name}, dòng {rowNumber}: không tải được ảnh VĐV \"{name}\".");

            var athlete = new Athlete
            {
                Id = Guid.NewGuid(),
                TeamId = team.Value,
                HoTen = name,
                NamSinh = birthYear,
                GioiTinh = gender.Value,
                NhomTuoi = ageGroup,
                AnhDaiDien = downloadedImage
            };
            _db.Athletes.Add(athlete);
            imported++;

            if (eventsColumn is not int eventIndex) continue;

            var registeredEventIds = new HashSet<Guid>();
            foreach (var eventName in SplitEventNames(CellText(row, eventIndex)))
            {
                var candidates = eventsByName[Normalize(eventName)].ToList();
                var matched = candidates.FirstOrDefault(x => x.NhomTuoi == ageGroup)
                    ?? candidates.FirstOrDefault(x => x.NhomTuoi == 0)
                    ?? (candidates.Count == 1 ? candidates[0] : null);

                if (matched is null)
                {
                    Warn($"Sheet {ws.Name}, dòng {rowNumber}: không tìm thấy nội dung \"{eventName}\" phù hợp Nhóm tuổi {ageGroup}.");
                    continue;
                }

                if (!registeredEventIds.Add(matched.Id)) continue;

                _db.Registrations.Add(new Registration
                {
                    Id = Guid.NewGuid(),
                    AthleteId = athlete.Id,
                    EventId = matched.Id
                });
                registrations++;
            }
        }

        return new ImportCount(imported, registrations, skipped, images);
    }

    private void Warn(string message)
    {
        if (_warnings.Count < MaxWarnings) _warnings.Add(message);
    }

    private static Guid? FindTeam(string nameOrCode, Dictionary<string, Guid> teams)
    {
        var key = Normalize(nameOrCode);
        return teams.TryGetValue(key, out var id) ? id : null;
    }

    private static string CellText(IXLRow row, int column)
        => Clean(row.Cell(column).GetFormattedString());

    private static int ParseInt(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : 0;
    }

    private static GioiTinh? ParseGender(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "nam" => GioiTinh.Nam,
            "nu" => GioiTinh.Nu,
            _ => null
        };
    }

    private static string? NormalizeRole(string value)
    {
        var text = Normalize(value);
        if (text.Contains("truongdoan")) return "truong_doan";
        if (text == "hlv" || text.Contains("huanluyenvien")) return "huan_luyen_vien";
        return null;
    }

    private static IEnumerable<string> SplitEventNames(string value)
        => value.Split([';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string AthleteKey(Guid teamId, string name, int birthYear)
        => $"{teamId:N}|{Normalize(name)}|{birthYear}";

    private static string StaffKey(Guid teamId, string name, string role)
        => $"{teamId:N}|{Normalize(name)}|{NormalizeRole(role) ?? Normalize(role)}";

    private static string Clean(string? value) => (value ?? "").Trim();

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        var text = value
            .Trim()
            .ToLowerInvariant()
            .Replace("đ", "d")
            .Normalize(NormalizationForm.FormD);

        var chars = text
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC);
    }

    private sealed record TeamImportResult(Dictionary<string, Guid> TeamIds, int Created, int Images);
    private sealed record ImportCount(int Imported, int Registrations, int Skipped, int Images);

    private sealed class HeaderMap
    {
        private readonly string _sheetName;
        private readonly Dictionary<string, int> _columns;

        private HeaderMap(string sheetName, Dictionary<string, int> columns)
        {
            _sheetName = sheetName;
            _columns = columns;
        }

        public static HeaderMap Create(IXLWorksheet ws)
        {
            var headerRow = ws.FirstRowUsed()
                ?? throw new InvalidDataException($"Sheet \"{ws.Name}\" đang trống.");
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.CellsUsed())
            {
                var key = Normalize(cell.GetFormattedString());
                if (!string.IsNullOrWhiteSpace(key)) columns[key] = cell.Address.ColumnNumber;
            }

            return new HeaderMap(ws.Name, columns);
        }

        public int? Find(params string[] aliases)
        {
            foreach (var alias in aliases)
                if (_columns.TryGetValue(Normalize(alias), out var index)) return index;
            return null;
        }

        public int Require(params string[] aliases)
            => Find(aliases)
               ?? throw new InvalidDataException(
                   $"Sheet \"{_sheetName}\" thiếu cột bắt buộc \"{aliases[0]}\".");
    }
}
