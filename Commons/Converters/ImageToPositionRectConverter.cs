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
                    && values[1] is long widthPosition
                    && values[2] is long widthRemaining)
                {
                    widthPosition = widthPosition > 0
                        ? widthPosition
                        : 0;

                    widthRemaining = widthRemaining > 0
                        ? widthRemaining
                        : 0;

                    if (widthPosition + widthRemaining > 0)
                    {
                        var width = (int)((bitmap.Size.Width * widthPosition) / (double)(widthPosition + widthRemaining));
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