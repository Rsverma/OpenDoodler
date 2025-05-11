using OpenBoardAnim.Models;

namespace OpenBoardAnim.Services
{
    public class StateSnapshotService
    {
        private readonly Stack<ProjectDetails> undoStack;
        private readonly Stack<ProjectDetails> redoStack;
        public StateSnapshotService()
        {
            undoStack = new Stack<ProjectDetails>();
            redoStack = new Stack<ProjectDetails>();
        }
        public void SaveState(ProjectDetails project)
        {
            if(undoStack.Count > 0)
            {
                var lastState = undoStack.Peek();
                if (lastState.Equals(project))
                    return;
            }
            undoStack.Push(project);
            redoStack.Clear();
        }
        public ProjectDetails Undo()
        {
            if (undoStack.Count > 0)
            {
                var state = undoStack.Pop();
                redoStack.Push(state);
                return state;
            }
            return null;
        }
        public ProjectDetails Redo()
        {
            if (redoStack.Count > 0)
            {
                var state = redoStack.Pop();
                undoStack.Push(state);
                return state;
            }
            return null;
        }
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}
