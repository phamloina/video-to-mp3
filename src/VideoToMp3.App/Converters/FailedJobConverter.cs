using System.Globalization;
using System.Windows.Data;
using VideoToMp3.Core.Models;

namespace VideoToMp3.App.Converters;

public sealed class FailedJobConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is ConversionJobStatus.Failed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
