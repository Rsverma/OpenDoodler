using OpenBoardAnim.AppConstants;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace OpenBoardAnim.Helpers
{
    public class EnumToItemSource : MarkupExtension
    {
        private readonly Type _type;

        public EnumToItemSource(Type type)
        {
            _type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_type.Equals(typeof(BoardStyle)))
                return Enum.GetValues(_type)
                .Cast<BoardStyle>()
                .Select(e => new { Value = (int)e, DisplayName = e.GetAttributeOfType<DescriptionAttribute>().Description, BrushColor = GetBrush(e) });
            if (_type.Equals(typeof(Resolution)))
                return Enum.GetValues(_type)
                .Cast<Resolution>()
                .Select(e => new { Value = (int)e, DisplayName = e.GetAttributeOfType<DescriptionAttribute>().Description });
            return Enum.GetValues(_type)
                .Cast<object>()
                .Select(e => new { Value = (int)e, DisplayName = e.ToString() });
        }

        private Brush GetBrush(BoardStyle style)
        {
            switch (style)
            {
                case BoardStyle.BlackBoard:
                    return Brushes.Black;

                case BoardStyle.GreenBoard:
                    return Brushes.Green;

                default:
                    return Brushes.White;
            }
        }
    }
}