using OpenBoardAnim.Models;
using System.Text.Json;

namespace OpenBoardAnim.Services
{
    public class StateSnapshotService
    {
        private readonly Stack<ProjectDetails> undoStack;
        private readonly Stack<ProjectDetails> redoStack;
        private ProjectDetails current;
        private string currentJson;

        public StateSnapshotService()
        {
            undoStack = new Stack<ProjectDetails>();
            redoStack = new Stack<ProjectDetails>();
        }

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public void SaveState(ProjectDetails project)
        {
            if (project == null) return;

            string json = JsonSerializer.Serialize(project);
            if (json == currentJson) return;

            if (current != null)
                undoStack.Push(current);
            current = project.Clone();
            currentJson = json;
            redoStack.Clear();
        }

        public ProjectDetails Undo()
        {
            if (!CanUndo) return null;

            redoStack.Push(current);
            current = undoStack.Pop();
            currentJson = JsonSerializer.Serialize(current);
            return current.Clone();
        }

        public ProjectDetails Redo()
        {
            if (!CanRedo) return null;

            undoStack.Push(current);
            current = redoStack.Pop();
            currentJson = JsonSerializer.Serialize(current);
            return current.Clone();
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            current = null;
            currentJson = null;
        }
    }
}
