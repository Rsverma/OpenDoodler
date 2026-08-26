namespace OpenBoardAnim.Library.Repositories
{
    public interface ISceneRepository
    {
        List<SceneTemplateEntity> GetAllTemplates();
        void AddTemplate(SceneTemplateEntity entity);
        void DeleteTemplate(int sceneTemplateID);
        void SeedBuiltInTemplatesIfNeeded(IEnumerable<(string Name, string SceneJson)> builtIns);
    }
}
