﻿namespace OpenBoardAnim.Util
{
    /// <summary>
    /// Animation export type.
    /// </summary>
    public enum Export
    {
        /// <summary>
        /// Graphics Interchange Format.
        /// </summary>
        Gif,

        /// <summary>
        /// Any type of video.
        /// </summary>
        Video,
    }

    /// <summary>
    /// The types of Panel of the Editor window.
    /// Positive values means that there's no preview overlay.
    /// </summary>
    public enum PanelType
    {
        /// <summary>
        /// Save As Panel.
        /// </summary>
        SaveAs = 1,

        /// <summary>
        /// New Animation Panel.
        /// </summary>
        NewAnimation = 2,

        /// <summary>
        /// Clipboard Panel.
        /// </summary>
        Clipboard = 3,

        /// <summary>
        /// Resize Panel.
        /// </summary>
        Resize = 4,

        /// <summary>
        /// Flip/Rotate Panel.
        /// </summary>
        FlipRotate = 5,

        /// <summary>
        /// Override Delay Panel.
        /// </summary>
        OverrideDelay = 6,

        /// <summary>
        /// Change Delay Panel.
        /// </summary>
        ChangeDelay = 7,

        /// <summary>
        /// Fade Transition Panel.
        /// </summary>
        Fade = 8,

        /// <summary>
        /// Slide Transition Panel.
        /// </summary>
        Slide = 9,

        /// <summary>
        /// Crop Panel.
        /// </summary>
        Crop = -1,

        /// <summary>
        /// Caption Panel.
        /// </summary>
        Caption = -2,

        /// <summary>
        /// Free Text Panel.
        /// </summary>
        FreeText = -3,

        /// <summary>
        /// Title Frame Panel.
        /// </summary>
        TitleFrame = -4,

        /// <summary>
        /// Free Drawing Panel.
        /// </summary>
        FreeDrawing = -5,

        /// <summary>
        /// Watermark Panel.
        /// </summary>
        Watermark = -6,

        /// <summary>
        /// Border Panel.
        /// </summary>
        Border = -7,

        /// <summary>
        /// Cinemagraph Panel.
        /// </summary>
        Cinemagraph = -8,

        /// <summary>
        /// Progress Panel.
        /// </summary>
        Progress = -9,
    }

    /// <summary>
    /// Transition animation.
    /// </summary>
    public enum SlideFrom
    {
        Right,
        Top,
        Left,
        Bottom
    }

    /// <summary>
    /// Stage status of the recording process.
    /// </summary>
    public enum Stage
    {
        /// <summary>
        /// Recording stopped.
        /// </summary>
        Stopped = 0,

        /// <summary>
        /// Recording active.
        /// </summary>
        Recording = 1,

        /// <summary>
        /// Recording paused.
        /// </summary>
        Paused = 2,

        /// <summary>
        /// Pre start countdown active.
        /// </summary>
        PreStarting = 3,

        /// <summary>
        /// Single shot mode.
        /// </summary>
        Snapping = 4
    }

    /// <summary>
    /// EncoderListBox Item Status.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Normal encoding status.
        /// </summary>
        Encoding,

        /// <summary>
        /// The Encoding was cancelled.
        /// </summary>
        Canceled,

        /// <summary>
        /// An error hapenned with the encoding process.
        /// </summary>
        Error,

        /// <summary>
        /// Encoding done.
        /// </summary>
        Completed,

        /// <summary>
        /// File deleted or Moved.
        /// </summary>
        FileDeletedOrMoved
    }

    /// <summary>
    /// Type of the Flip/Rotate action.
    /// </summary>
    public enum FlipRotateType
    {
        FlipHorizontal,
        FlipVertical,
        RotateRight90,
        RotateLeft90,
    }

    /// <summary>
    /// Exit actions after closing the Recording Window.
    /// </summary>
    public enum ExitAction
    {
        /// <summary>
        /// Return to the StartUp Window.
        /// </summary>
        Return = 0,

        /// <summary>
        /// Something was recorded. Go to the Editor.
        /// </summary>
        Recorded = 1,

        /// <summary>
        /// Exit the application.
        /// </summary>
        Exit = 2,
    }

    /// <summary>
    /// Type of delay change action.
    /// </summary>
    public enum DelayChangeType
    {
        Override,
        IncreaseDecrease
    }

    /// <summary>
    /// Type of the gif encoder.
    /// </summary>
    public enum GifEncoderType
    {
        Legacy,
        OpenBoardAnim,
        PaintNet
    }

    /// <summary>
    /// Type of the video encoder.
    /// </summary>
    public enum VideoEncoderType
    {
        AviStandalone,
        Ffmpg,
    }

    /// <summary>
    /// Type of the progress indicator.
    /// </summary>
    public enum ProgressType
    {
        Bar,
        Text,
    }

    /// <summary>
    /// The type of directory, used to decide the icon of the folder inside the SelectFolderDialog.
    /// </summary>
    public enum DirectoryType
    {
        ThisComputer,
        Drive,
        Folder,
        File,

        Desktop,
        Documents,
        Images,
        Music,
        Videos,
        Downloads
    }
}
