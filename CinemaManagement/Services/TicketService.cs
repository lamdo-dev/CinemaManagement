using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;

namespace CinemaManagement.Services;

/// <summary>
/// Phục vụ tra cứu phim, lịch chiếu và sơ đồ ghế cho luồng đặt vé của khách hàng.
/// Giúp tầng giao diện (Customer UserControls) không phải gọi trực tiếp xuống Repository.
/// </summary>
public class TicketService
{
    private readonly MovieRepository _movieRepository = new();
    private readonly RoomRepository _roomRepository = new();
    private readonly ShowtimeRepository _showtimeRepository = new();
    private readonly TicketRepository _ticketRepository = new();
    private readonly PricingService _pricingService = new();

    /// <summary>
    /// Lấy danh sách phim hiển thị cho khách hàng (Đang chiếu & Sắp chiếu, ẩn phim Đã kết thúc).
    /// </summary>
    public List<Movie> GetCustomerMovies()
    {
        return _movieRepository.GetAll()
            .Where(m => m.Status == MovieStatus.Showing || m.Status == MovieStatus.ComingSoon)
            .ToList();
    }

    /// <summary>
    /// Lấy danh sách suất chiếu khả dụng theo ngày (và theo phim nếu có),
    /// kèm thông tin Phim, Phòng và giá vé khởi điểm đã áp dụng PricingRules.
    /// </summary>
    public List<(Showtime Showtime, Movie Movie, Room? Room, decimal StandardPrice)> GetAvailableShowtimes(
        DateTime selectedDate,
        int? movieId = null)
    {
        DateTime now = DateTime.Now;
        List<Showtime> showtimes = _showtimeRepository.GetUpcomingByDate(
            selectedDate.Date,
            now,
            movieId);

        if (showtimes.Count == 0)
        {
            return new List<(Showtime, Movie, Room?, decimal)>();
        }

        Dictionary<int, Movie> movieMap = _movieRepository.GetAll()
            .Where(m => m.Status != MovieStatus.Ended)
            .ToDictionary(m => m.Id);

        Dictionary<long, Room> roomMap = _roomRepository.GetAll()
            .ToDictionary(r => r.Id);

        List<PricingRule> pricingRules;
        try
        {
            pricingRules = _pricingService.GetAll();
        }
        catch
        {
            pricingRules = new List<PricingRule>();
        }

        var result = new List<(Showtime, Movie, Room?, decimal)>(showtimes.Count);

        foreach (Showtime showtime in showtimes)
        {
            if (!movieMap.TryGetValue(showtime.MovieId, out Movie? movie))
                continue;

            roomMap.TryGetValue(showtime.RoomId, out Room? room);

            decimal standardPrice = _pricingService.CalculatePriceBySeatType(
                showtime,
                SeatType.Standard,
                1.0,
                pricingRules);

            result.Add((showtime, movie, room, standardPrice));
        }

        return result;
    }

    /// <summary>
    /// Tải toàn bộ thông tin phòng, ghế, ghế đã bán và bảng giá thực tế cho màn hình chọn ghế.
    /// </summary>
    public (
        Room? Room,
        List<Seat> RoomSeats,
        HashSet<string> OccupiedSeats,
        Dictionary<string, decimal> SeatPriceMap,
        List<PricingRule> PricingRules) GetSeatSelectionData(Showtime showtime)
    {
        if (showtime == null)
            throw new ArgumentNullException(nameof(showtime));

        Room? room = _roomRepository.GetById(showtime.RoomId);
        if (room == null)
        {
            return (
                null,
                new List<Seat>(),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
                new List<PricingRule>());
        }

        List<Seat> roomSeats = _roomRepository.GetSeatsByRoomId(room.Id);
        HashSet<string> occupiedSeats = _ticketRepository.GetBookedSeatNumbers(showtime.Id);

        List<PricingRule> pricingRules;
        try
        {
            pricingRules = _pricingService.GetAll();
        }
        catch
        {
            pricingRules = new List<PricingRule>();
        }

        var seatPriceMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (Seat seat in roomSeats)
        {
            if (string.IsNullOrWhiteSpace(seat.SeatNumber))
                continue;

            seatPriceMap[seat.SeatNumber.Trim()] = _pricingService.CalculateSeatPrice(
                showtime,
                seat,
                pricingRules);
        }

        return (room, roomSeats, occupiedSeats, seatPriceMap, pricingRules);
    }

    public HashSet<string> GetBookedSeatNumbers(long showtimeId)
    {
        return _ticketRepository.GetBookedSeatNumbers(showtimeId);
    }
}
