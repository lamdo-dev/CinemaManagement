
using Dapper;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Repositories;

public class MovieRepository
{
    // 1. Lấy toàn bộ danh sách phim
    public List<Movie> GetAll()
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            SELECT
                Id,
                Title,
                Description,
                DurationMinutes,
                Director,
                Genre,
                PosterUrl,
                ReleaseDate,
                Status
            FROM Movies
            ORDER BY Id DESC;
        ";

        return connection.Query<Movie>(sql).ToList();
    }

    // 2. Tìm phim theo ID
    public Movie? GetById(int id)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            SELECT
                Id,
                Title,
                Description,
                DurationMinutes,
                Director,
                Genre,
                PosterUrl,
                ReleaseDate,
                Status
            FROM Movies
            WHERE Id = @Id;
        ";

        return connection.QueryFirstOrDefault<Movie>(
            sql,
            new { Id = id }
        );
    }

    // 3. Thêm phim mới
    public int Add(Movie movie)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            INSERT INTO Movies (
                Title,
                Description,
                DurationMinutes,
                Director,
                Genre,
                PosterUrl,
                ReleaseDate,
                Status
            )
            VALUES (
                @Title,
                @Description,
                @DurationMinutes,
                @Director,
                @Genre,
                @PosterUrl,
                @ReleaseDate,
                @Status
            );

            SELECT last_insert_rowid();
        ";

        return connection.ExecuteScalar<int>(sql, new
        {
            movie.Title,
            movie.Description,
            movie.DurationMinutes,
            movie.Director,
            movie.Genre,
            movie.PosterUrl,
            ReleaseDate = movie.ReleaseDate.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = (int)movie.Status
        });
    }

    // 4. Cập nhật thông tin phim
    public bool Update(Movie movie)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            UPDATE Movies
            SET
                Title = @Title,
                Description = @Description,
                DurationMinutes = @DurationMinutes,
                Director = @Director,
                Genre = @Genre,
                PosterUrl = @PosterUrl,
                ReleaseDate = @ReleaseDate,
                Status = @Status
            WHERE Id = @Id;
        ";

        int rowsAffected = connection.Execute(sql, new
        {
            movie.Id,
            movie.Title,
            movie.Description,
            movie.DurationMinutes,
            movie.Director,
            movie.Genre,
            movie.PosterUrl,
            ReleaseDate = movie.ReleaseDate.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = (int)movie.Status
        });

        return rowsAffected > 0;
    }

    // 5. Xóa phim theo ID
    public bool Delete(int id)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            DELETE FROM Movies
            WHERE Id = @Id;
        ";

        int rowsAffected = connection.Execute(sql, new { Id = id });

        return rowsAffected > 0;
    }

    // 6. Tìm kiếm phim theo tên, đạo diễn hoặc thể loại
    public List<Movie> Search(string keyword)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        const string sql = @"
            SELECT
                Id,
                Title,
                Description,
                DurationMinutes,
                Director,
                Genre,
                PosterUrl,
                ReleaseDate,
                Status
            FROM Movies
            WHERE
                Title LIKE @Keyword
                OR Director LIKE @Keyword
                OR Genre LIKE @Keyword
            ORDER BY Id DESC;
        ";

        return connection.Query<Movie>(sql, new
        {
            Keyword = $"%{keyword}%"
        }).ToList();
    }
}
