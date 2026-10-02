using Microsoft.UI.Xaml.Data;
namespace Yume.Views;
public sealed class VolumePercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => $"{Math.Round(System.Convert.ToDouble(value))}%";
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
