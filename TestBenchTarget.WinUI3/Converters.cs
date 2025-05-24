using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;

namespace TestBenchTarget.WinUI3
{
    /// <summary>
    /// Converter pre WinUI3 ktorý dokáže získať formát z ViewModelu
    /// </summary>
    public sealed class DynamicDateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                if (value is DateTime dateTime)
                {
                    // Parameter môže byť formát priamo z XAML
                    if (parameter is string format && !string.IsNullOrEmpty(format))
                    {
                        return dateTime.ToString(format);
                    }

                    // Skúsiť získať formát z DataContext (MainViewModel)
                    if (Application.Current is App app &&
                        app.m_window is MainWindow mainWindow &&
                        mainWindow.ViewModel != null)
                    {
                        return dateTime.ToString(mainWindow.ViewModel.DateFormat);
                    }

                    // Záložný formát
                    return dateTime.ToString("dd.MM.yyyy");
                }
                return value?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DynamicDateFormatConverter: {ex.Message}");
                return value?.ToString() ?? string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object? parameter, string? language)
        {
            try
            {
                if (value is string dateString)
                {
                    // Skúsiť parseovať podľa rôznych formátov
                    foreach (var format in new[] { "dd.MM.yyyy", "MM/dd/yyyy", "yyyy-MM-dd" })
                    {
                        if (DateTime.TryParseExact(dateString, format, null,
                            System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                        {
                            return parsedDate;
                        }
                    }

                    // Skúsiť obyčajný parsing ako poslednú možnosť
                    if (DateTime.TryParse(dateString, out DateTime result))
                    {
                        return result;
                    }
                }
                return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Originalny converter pre spätnosť
    /// </summary>
    public sealed class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                if (value is DateTime dateTime)
                {
                    // Alternatívne použiť parameter, ak bol poskytnutý
                    if (parameter is string format)
                    {
                        return dateTime.ToString(format);
                    }

                    // Záložný formát
                    return dateTime.ToString("dd.MM.yyyy");
                }
                return value?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in date conversion: {ex.Message}");
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object? parameter, string? language)
        {
            try
            {
                if (value is string dateString)
                {
                    // Skúsiť parseovať podľa rôznych formátov
                    foreach (var format in new[] { "dd.MM.yyyy", "MM/dd/yyyy", "yyyy-MM-dd" })
                    {
                        if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                        {
                            return parsedDate;
                        }
                    }

                    // Skúsiť obyčajný parsing ako poslednú možnosť
                    if (DateTime.TryParse(dateString, out DateTime result))
                    {
                        return result;
                    }
                }
                return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }
}
