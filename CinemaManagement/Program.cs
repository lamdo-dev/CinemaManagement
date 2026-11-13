using System;
using System.Windows.Forms;
using CinemaManagement.Data;

namespace CinemaManagement;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        DatabaseInitializer.Initialize();

        // Khởi động vào Form Đăng nhập trước
        Application.Run(new LoginForm());
    }
}