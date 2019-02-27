using MaterialDesignThemes.Wpf;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace logviewer.core
{
    public class EnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.ToObject((Type)parameter, (int)value);
        }
    }

    public class EnumToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumType = parameter as Type;
            if (enumType == null || !enumType.IsEnum)
            {
                throw new ArgumentException("Invalid enumeration type");
            }

            var enumMember = enumType.GetMember(value.ToString()).FirstOrDefault();
            if (enumMember == null)
            {
                return string.Empty;
            }

            var enumAttribute = enumMember.GetCustomAttributes(typeof(EnumConverterAttribute), false).Cast<EnumConverterAttribute>().FirstOrDefault();
            if (enumAttribute != null)
            {
                return enumAttribute.Label;
            }
            else
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumType = parameter as Type;
            if (enumType == null || !enumType.IsEnum)
            {
                throw new ArgumentException("Invalid enumeration type");
            }

            if (value == null)
            {
                return PackIconKind.Alert;
            }

            var enumMember = enumType.GetMember(value.ToString()).FirstOrDefault();
            if (enumMember == null)
            {
                return PackIconKind.Alert;
            }

            var enumAttribute = enumMember.GetCustomAttributes(typeof(EnumConverterAttribute), false).Cast<EnumConverterAttribute>().FirstOrDefault();
            if (enumAttribute != null)
            {
                return enumAttribute.Icon;
            }
            else
            {
                return PackIconKind.Alert;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumConverterAttribute : Attribute
    {
        public EnumConverterAttribute(PackIconKind icon)
        {
            Icon = icon;
        }

        public EnumConverterAttribute(string label)
        {
            Label = label;
        }

        public EnumConverterAttribute(PackIconKind icon, string label)
        {
            Icon = icon;
            Label = label;
        }

        public PackIconKind Icon
        {
            get;
            private set;
        }

        public string Label
        {
            get;
            private set;
        }
    }
}
