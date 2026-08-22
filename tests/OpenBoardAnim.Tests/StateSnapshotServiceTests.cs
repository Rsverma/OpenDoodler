using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class StateSnapshotServiceTests
    {
        [Fact]
        public void Initially_CannotUndoOrRedo()
        {
            StateSnapshotService sut = new();

            Assert.False(sut.CanUndo);
            Assert.False(sut.CanRedo);
        }

        [Fact]
        public void SaveState_Null_IsNoOp()
        {
            StateSnapshotService sut = new();

            sut.SaveState(null);

            Assert.False(sut.CanUndo);
        }

        [Fact]
        public void SaveState_FirstCall_DoesNotEnableUndo()
        {
            StateSnapshotService sut = new();

            sut.SaveState(new ProjectDetails { Title = "First" });

            Assert.False(sut.CanUndo); // nothing to undo back to yet
        }

        [Fact]
        public void SaveState_SecondDifferentCall_EnablesUndo()
        {
            StateSnapshotService sut = new();
            sut.SaveState(new ProjectDetails { Title = "First" });

            sut.SaveState(new ProjectDetails { Title = "Second" });

            Assert.True(sut.CanUndo);
        }

        [Fact]
        public void SaveState_IdenticalContentTwice_DoesNotPushADuplicate()
        {
            StateSnapshotService sut = new();
            // CreatedOn defaults to DateTime.Now, so it's pinned explicitly here - otherwise
            // two separately-constructed instances would never serialize identically.
            DateTime createdOn = new(2026, 1, 1);
            sut.SaveState(new ProjectDetails { Title = "Same", CreatedOn = createdOn, Scenes = [] });

            sut.SaveState(new ProjectDetails { Title = "Same", CreatedOn = createdOn, Scenes = [] });

            Assert.False(sut.CanUndo);
        }

        [Fact]
        public void Undo_WhenNothingToUndo_ReturnsNull()
        {
            StateSnapshotService sut = new();

            Assert.Null(sut.Undo());
        }

        [Fact]
        public void Undo_ReturnsThePreviousState_AndEnablesRedo()
        {
            StateSnapshotService sut = new();
            sut.SaveState(new ProjectDetails { Title = "First" });
            sut.SaveState(new ProjectDetails { Title = "Second" });

            ProjectDetails restored = sut.Undo();

            Assert.Equal("First", restored.Title);
            Assert.True(sut.CanRedo);
        }

        [Fact]
        public void Redo_WhenNothingToRedo_ReturnsNull()
        {
            StateSnapshotService sut = new();

            Assert.Null(sut.Redo());
        }

        [Fact]
        public void Redo_AfterUndo_ReturnsBackToTheLaterState()
        {
            StateSnapshotService sut = new();
            sut.SaveState(new ProjectDetails { Title = "First" });
            sut.SaveState(new ProjectDetails { Title = "Second" });
            sut.Undo();

            ProjectDetails restored = sut.Redo();

            Assert.Equal("Second", restored.Title);
            Assert.True(sut.CanUndo);
            Assert.False(sut.CanRedo);
        }

        [Fact]
        public void SaveState_AfterUndo_ClearsTheRedoStack()
        {
            StateSnapshotService sut = new();
            sut.SaveState(new ProjectDetails { Title = "First" });
            sut.SaveState(new ProjectDetails { Title = "Second" });
            sut.Undo();
            Assert.True(sut.CanRedo);

            sut.SaveState(new ProjectDetails { Title = "Branch" });

            Assert.False(sut.CanRedo);
        }

        [Fact]
        public void Undo_ReturnsAClone_NotTheOriginalInstance()
        {
            StateSnapshotService sut = new();
            ProjectDetails first = new() { Title = "First" };
            sut.SaveState(first);
            sut.SaveState(new ProjectDetails { Title = "Second" });

            ProjectDetails restored = sut.Undo();

            Assert.NotSame(first, restored);
        }

        [Fact]
        public void Clear_ResetsUndoAndRedoAvailability()
        {
            StateSnapshotService sut = new();
            sut.SaveState(new ProjectDetails { Title = "First" });
            sut.SaveState(new ProjectDetails { Title = "Second" });
            sut.Undo();

            sut.Clear();

            Assert.False(sut.CanUndo);
            Assert.False(sut.CanRedo);
        }

        [Fact]
        public void Clear_ThenSaveState_BehavesLikeAFreshBaseline()
        {
            StateSnapshotService sut = new();
            sut.SaveState(new ProjectDetails { Title = "First" });
            sut.SaveState(new ProjectDetails { Title = "Second" });
            sut.Clear();

            sut.SaveState(new ProjectDetails { Title = "Third" });

            Assert.False(sut.CanUndo); // treated as the first save again, nothing to undo to
        }

        [Fact]
        public void MultipleUndos_WalkBackThroughEachSavedState()
        {
            StateSnapshotService sut = new();
            sut.SaveState(new ProjectDetails { Title = "A" });
            sut.SaveState(new ProjectDetails { Title = "B" });
            sut.SaveState(new ProjectDetails { Title = "C" });

            Assert.Equal("B", sut.Undo().Title);
            Assert.Equal("A", sut.Undo().Title);
            Assert.False(sut.CanUndo);
        }
    }
}
