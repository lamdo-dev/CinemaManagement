using CineworldApp.Data;
using CineworldApp.Forms;
using CineworldApp.Services;

namespace CineworldApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Khởi tạo repository và service
            var userRepository = new UserRepository();
            var authService = new AuthService(userRepository);

            // Chạy form đăng nhập
            Application.Run(new LoginForm(authService));
        }
    }
}
