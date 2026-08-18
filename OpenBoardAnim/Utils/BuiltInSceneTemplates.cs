using OpenBoardAnim.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace OpenBoardAnim.Utils
{
    // Hand-authored starter-scene compositions for the built-in template gallery (see
    // CacheService.LoadSceneTemplates) - each combines a bundled peep-*.svg character with a
    // built-in shape and/or a text placeholder into a small preset layout, using only assets
    // that already ship with the app (no user library/database dependency), so the gallery has
    // real content on a completely fresh install. Positions assume the default 960x540 (16:9)
    // editor canvas - a reasonable default even though the actual canvas size can vary by
    // aspect ratio, consistent with how graphic placement works everywhere else in the app
    // (there's no relative/percentage positioning system anywhere to hook into instead).
    public static class BuiltInSceneTemplates
    {
        // shapes is CacheService.AllShapes (the built-in shape catalog, already loaded from
        // ShapeRepository) - reused here rather than re-fetching it separately.
        public static List<(string Name, string SceneJson)> GetAll(IEnumerable<DrawingModel> shapes)
        {
            DrawingModel speechBubble = shapes.FirstOrDefault(s => s.Name == "Speech-Bubble");
            DrawingModel thoughtBubble = shapes.FirstOrDefault(s => s.Name == "Thought-Bubble");

            return new List<(string, string)>
            {
                ("Character Speaking", Serialize(CharacterSpeaking(speechBubble))),
                ("Character Thinking", Serialize(CharacterThinking(thoughtBubble))),
                ("Character with Caption", Serialize(CharacterWithCaption())),
                ("Title Intro", Serialize(TitleIntro())),
            };
        }

        private static string Serialize(SceneModel scene) => JsonSerializer.Serialize(scene);

        private static string ReadPeepSvg(string fileName) =>
            File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName));

        private static DrawingModel Peep(string fileName, double x, double y, double delay) => new()
        {
            Name = "Character",
            SVGText = ReadPeepSvg(fileName),
            X = x,
            Y = y,
            Delay = delay,
            Duration = 1.5
        };

        private static DrawingModel Shape(DrawingModel shape, double x, double y, double delay) => new()
        {
            Name = shape?.Name ?? "Shape",
            SVGText = shape?.SVGText,
            X = x,
            Y = y,
            Delay = delay,
            Duration = 1
        };

        private static TextModel Caption(string text, double x, double y, double delay, double fontSize = 24) => new()
        {
            RawText = text,
            SelectedFontFamilyString = "Segoe UI",
            SelectedFontStyle = FontStyles.Normal,
            SelectedFontWeight = FontWeights.Normal,
            SelectedFontSize = fontSize,
            X = x,
            Y = y,
            Delay = delay,
            Duration = 1
        };

        private static SceneModel CharacterSpeaking(DrawingModel speechBubble)
        {
            SceneModel scene = new() { Name = "Character Speaking" };
            scene.Graphics.Add(Peep("peep-102.svg", 350, 150, 0));
            scene.Graphics.Add(Shape(speechBubble, 560, 70, 1.5));
            scene.Graphics.Add(Caption("Hello!", 585, 105, 2.5));
            return scene;
        }

        private static SceneModel CharacterThinking(DrawingModel thoughtBubble)
        {
            SceneModel scene = new() { Name = "Character Thinking" };
            scene.Graphics.Add(Peep("peep-64.svg", 350, 150, 0));
            scene.Graphics.Add(Shape(thoughtBubble, 560, 60, 1.5));
            scene.Graphics.Add(Caption("Hmm...", 585, 95, 2.5));
            return scene;
        }

        private static SceneModel CharacterWithCaption()
        {
            SceneModel scene = new() { Name = "Character with Caption" };
            scene.Graphics.Add(Peep("peep-43.svg", 380, 90, 0));
            scene.Graphics.Add(Caption("Your text here", 300, 430, 1.5, 28));
            return scene;
        }

        private static SceneModel TitleIntro()
        {
            SceneModel scene = new() { Name = "Title Intro" };
            scene.Graphics.Add(Caption("Your Title Here", 260, 60, 0, 40));
            scene.Graphics.Add(Peep("peep-61.svg", 380, 180, 1.5));
            return scene;
        }
    }
}
