using Avalonia;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SportDJ.Commons.Converters
{
    public class ImageToPositionRectConverter
        : OneWayMultiValueConverter
    {
        #region Public Methods

        public override object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            var result = default(PixelRect);

            if (values?.Any() == true
                && values[0] is Bitmap bitmap)
            {
                if (values?.Count >= 3
                    && values[1] is double played
                    && values[2] is double remaining)
                {
                    played = played > 0
                        ? played
                        : 0;

                    remaining = remaining > 0
                        ? remaining
                        : 0;

                    if (played + remaining > 0)
                    {
                        var width = (int)(bitmap.Size.Width * played / (played + remaining));
                        var height = (int)bitmap.Size.Height;

                        result = new PixelRect(
                            x: 0,
                            y: 0,
                            width: width,
                            height: height);
                    }
                }

                if (result == default)
                {
                    result = new PixelRect(
                        size: bitmap.PixelSize);
                }
            }

            return result;
        }

        #endregion Public Methods
    }
}