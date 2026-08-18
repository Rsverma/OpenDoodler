using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Library.Repositories
{
    public class SceneRepository
    {
        private readonly Func<DataContext> _contextFactory;

        public SceneRepository(Func<DataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public List<SceneTemplateEntity> GetAllTemplates()
        {
            try
            {
                using var context = _contextFactory();
                return [.. context.SceneTemplates
                        .OrderByDescending(t => t.IsBuiltIn)
                        .ThenBy(t => t.SceneTemplateID)];
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return new List<SceneTemplateEntity>();
        }

        public void AddTemplate(SceneTemplateEntity entity)
        {
            try
            {
                using var context = _contextFactory();
                context.SceneTemplates.Add(entity);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public void DeleteTemplate(int sceneTemplateID)
        {
            try
            {
                using var context = _contextFactory();
                SceneTemplateEntity entity = context.SceneTemplates.Find(sceneTemplateID);
                if (entity != null)
                {
                    context.SceneTemplates.Remove(entity);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        // Seeds the built-in starter gallery exactly once - safe to call on every startup,
        // it's a no-op once any built-in row already exists.
        public void SeedBuiltInTemplatesIfNeeded(IEnumerable<(string Name, string SceneJson)> builtIns)
        {
            try
            {
                using var context = _contextFactory();
                if (context.SceneTemplates.Any(t => t.IsBuiltIn)) return;
                foreach ((string name, string json) in builtIns)
                {
                    context.SceneTemplates.Add(new SceneTemplateEntity
                    {
                        Name = name,
                        SceneJson = json,
                        IsBuiltIn = true
                    });
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
