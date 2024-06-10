using OpenBoardAnim.Core;
using OpenBoardAnim.Services;

namespace OpenBoardAnim.ViewModels
{
    public class EditorActionsViewModel : ViewModel
    {
        private IPubSubService _pubSub;

        public EditorActionsViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
        }
    }
}
