using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for EditorLibraryView.xaml
    /// </summary>
    public partial class EditorLibraryView : UserControl
    {
        public EditorLibraryView()
        {
            InitializeComponent();
        }

        // The graphics ListBox sits inside GraphicsScrollViewer (which is what should actually
        // scroll, since it also covers the "Load More" button below the list) - even with the
        // ListBox's own ScrollViewer.VerticalScrollBarVisibility set to Disabled, its internal
        // ScrollViewer still swallows MouseWheel before it bubbles to the outer one. Scroll the
        // outer ScrollViewer directly instead of relying on bubbling.
        private void GraphicsListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                GraphicsScrollViewer.ScrollToVerticalOffset(GraphicsScrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void TextColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not EditorLibraryViewModel viewModel) return;
                if (sender is Button button && button.Tag is string hex)
                    viewModel.SelectedTextColorHex = hex;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void BoldToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.ToggleBold.Execute(null, textRichTextBox);
                textRichTextBox.Focus();
                UpdateFormatToggleState();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ItalicToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EditingCommands.ToggleItalic.Execute(null, textRichTextBox);
                textRichTextBox.Focus();
                UpdateFormatToggleState();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void UnderlineToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleTextDecoration(TextDecorationLocation.Underline);
                textRichTextBox.Focus();
                UpdateFormatToggleState();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void StrikethroughToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleTextDecoration(TextDecorationLocation.Strikethrough);
                textRichTextBox.Focus();
                UpdateFormatToggleState();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Underline and Strikethrough both live on the same Inline.TextDecorations collection
        // property (unlike Bold/Italic, which are independent properties), so there's no built-in
        // EditingCommands.ToggleStrikethrough to lean on - toggle just the one requested location
        // within whatever collection the selection already has, preserving the other.
        private void ToggleTextDecoration(TextDecorationLocation location)
        {
            object current = textRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            TextDecorationCollection existing = current as TextDecorationCollection ?? new TextDecorationCollection();
            bool hasDecoration = existing.Any(d => d.Location == location);
            TextDecorationCollection updated = new(existing.Where(d => d.Location != location));
            if (!hasDecoration)
                updated.Add(new TextDecoration { Location = location });
            textRichTextBox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, updated);
        }

        private void TextRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateFormatToggleState();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // AddTextCommand's CanExecute checks RawText, which otherwise only gets populated in
        // AddText_Click - a disabled Button never raises Click, so without this the button would
        // stay disabled forever (it can't be clicked to populate the very property that would
        // enable it). Keep RawText live as the user types so CanExecute (and therefore
        // Button.IsEnabled, via the Command binding) tracks the RichTextBox correctly.
        private void TextRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (DataContext is not EditorLibraryViewModel viewModel) return;
                (string rawText, _) = ExtractTextAndFormatRuns(textRichTextBox);
                viewModel.RawText = rawText;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Reflects the current selection's bold/italic/underline/strikethrough state on the
        // toolbar buttons - a mixed selection (spanning both bold and non-bold runs, say) reports
        // DependencyProperty.UnsetValue, which the pattern match below treats as "not checked".
        private void UpdateFormatToggleState()
        {
            object weight = textRichTextBox.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            object style = textRichTextBox.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            object decorations = textRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            boldToggle.IsChecked = weight is FontWeight fontWeight && fontWeight == FontWeights.Bold;
            italicToggle.IsChecked = style is FontStyle fontStyle && fontStyle == FontStyles.Italic;
            TextDecorationCollection currentDecorations = decorations as TextDecorationCollection;
            underlineToggle.IsChecked = currentDecorations != null && currentDecorations.Any(d => d.Location == TextDecorationLocation.Underline);
            strikethroughToggle.IsChecked = currentDecorations != null && currentDecorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);
        }

        // Runs before AddTextCommand executes (Button.Click fires before its bound Command) -
        // syncs the ViewModel's RawText/FormatRuns from the RichTextBox's FlowDocument, since a
        // RichTextBox's per-run formatting can't be captured via a plain TwoWay text binding.
        private void AddText_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not EditorLibraryViewModel viewModel) return;
                (string rawText, List<TextFormatRun> formatRuns) = ExtractTextAndFormatRuns(textRichTextBox);
                viewModel.RawText = rawText;
                viewModel.FormatRuns = formatRuns;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private static (string RawText, List<TextFormatRun> FormatRuns) ExtractTextAndFormatRuns(RichTextBox richTextBox)
        {
            StringBuilder text = new();
            List<TextFormatRun> formatRuns = new();
            bool firstParagraph = true;
            foreach (Block block in richTextBox.Document.Blocks)
            {
                if (block is not Paragraph paragraph) continue;
                if (!firstParagraph) text.Append('\n');
                firstParagraph = false;
                foreach (Inline inline in paragraph.Inlines)
                {
                    if (inline is Run run && !string.IsNullOrEmpty(run.Text))
                    {
                        int start = text.Length;
                        text.Append(run.Text);
                        formatRuns.Add(new TextFormatRun
                        {
                            Start = start,
                            Length = run.Text.Length,
                            IsBold = run.FontWeight == FontWeights.Bold,
                            IsItalic = run.FontStyle == FontStyles.Italic,
                            IsUnderline = run.TextDecorations != null && run.TextDecorations.Any(d => d.Location == TextDecorationLocation.Underline),
                            IsStrikethrough = run.TextDecorations != null && run.TextDecorations.Any(d => d.Location == TextDecorationLocation.Strikethrough)
                        });
                    }
                    else if (inline is LineBreak)
                    {
                        text.Append('\n');
                    }
                }
            }
            return (text.ToString(), formatRuns);
        }
    }
}
