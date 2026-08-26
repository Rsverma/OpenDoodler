namespace OpenBoardAnim.Models
{
    // A contiguous slice of TextModel.RawText (by character index) carrying its own bold/italic
    // state, independent of the text's base SelectedFontStyle/SelectedFontWeight.
    public class TextFormatRun
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUnderline { get; set; }
        public bool IsStrikethrough { get; set; }
    }
}
